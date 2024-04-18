#!/usr/bin/env python
# -*- encoding: UTF-8 -*-

# Note that the –pip and –pport option are automatically set by NAOqi while it runs the script.

import qi
import time
import sys
import argparse
import os

import json
import random
import datetime
import logging
import ConfigParser
from naoqi import ALProxy

from mirroring.udp_socket import UDPClient
from mirroring.motion_handler import MotionHandler
from mirroring.audio_handler import AudioHandler



class MirrorClient(object):
    """
    Our main class for handling the mirroring game events.
    """

    def __init__(self, app, configFile):
        """
        Initialisation of qi framework and event detection.
        """
        super(MirrorClient, self).__init__()
        self.app = app
        self.app.start()
        self.session = self.app.session

        config = ConfigParser.SafeConfigParser()
        config.readfp(open(configFile))
        #config.read(['site.cfg', os.path.expanduser('~/.myapp.cfg')])

        ComputerPort = config.getint("Networking", "ComputerPort")

        keepAliveTime = config.getint("Networking", "keepAliveTime")
        self.ModeTopic = config.get("Networking", "ModeTopic")
        self.JointTopic = config.get("Networking", "JointTopic")
        self.AudioTopic = config.get("Networking", "AudioTopic")
        self.SayTopic = config.get("Networking", "SayTopic")
        self.LogTopic = config.get("Networking", "LogTopic")

        logPath = config.get("Logging","LogFilePath")
        self.audioPath = config.get("Logging","AudioFilePath")

        self.udp_client = UDPClient(ComputerPort)

        # Example log formatting
        # FORMAT = '%(asctime)s %(clientip)-15s %(user)-8s %(message)s'
        # logging.basicConfig(format=FORMAT)
        # d = {'clientip': '192.168.0.1', 'user': 'fbloggs'}
        # logging.warning('Protocol problem: %s', 'connection reset', extra=d)
        ## Output
        # 2006-02-08 22:20:02,165 192.168.0.1 fbloggs  Protocol problem: connection reset

        # We want to log our results
        # Log line format: yyyy-mm-dd HH:MM:SS,sss,  <string>
        # where time is 24-hour and sss is miliseconds
        FORMAT = '%(asctime)s,   %(message)s'
        formatter = logging.Formatter(fmt=FORMAT)
        # Uncomment this one for for shorter time format
        # Log line format: HH:MM:SS,   <string>
        #formatter = logging.Formatter(fmt=FORMAT, datefmt='%H:%M:%S')

        self.logger = logging.getLogger('logger')
        self.logger.setLevel(logging.DEBUG)

        # Monitor logging
        # ch = logging.StreamHandler()
        # ch.setLevel(logging.INFO)
        # ch.setFormatter(formatter)
        # logger.addHandler(ch)

        # File logging
        # Path format: path/to/log/folder/yyyy-mm-dd_HHMM.log
        PATHFORMAT = '{0}{1:04d}-{2:02d}-{3:02d}_{4:02d}{5:02d}.log'
        dt = datetime.datetime.now()
        logName = PATHFORMAT.format(logPath, dt.year, dt.month, dt.day ,dt.hour, dt.minute)

        fh = logging.FileHandler(logName, mode='w', encoding='utf-8')
        fh.setLevel(logging.DEBUG)
        fh.setFormatter(formatter)
        self.logger.addHandler(fh)

        self.newPacket = False
        self.packetData = []

        # Current robot mode
        # -1 - Do nothing
        #  0 - Lead
        #  1 - Follow
        self.currentMode = -1
        self.lastMode = -1
        self.startNewMode = False

        # Which participant are we currently following?
        self.followParticipant = 0

        # Tracking our joint angle data
        self.jointNames = ["RShoulderPitch", "RShoulderRoll", "RElbowRoll", "RElbowYaw",  "LShoulderPitch", "LShoulderRoll", "LElbowRoll", "LElbowYaw"]

        # Tracking what behavior was or is currently running
        self.startedBehavior = False
        self.currentBehavior = ""

        # Setup our handlers
        self.motion_handler = MotionHandler(self.session, self.logger)
        self.audio_handler = AudioHandler(self.session, self.logger, self.audioPath)


    def initPose(self):
        motion_service  = self.session.service("ALMotion")
        posture_service = self.session.service("ALRobotPosture")
        #motion_service.setStiffnesses("Body", 0.0)
        #posture_service.goToPosture("StandInit", 0.5)
        motion_service.setSmartStiffnessEnabled(True)
        motion_service.wakeUp()
        self.isStiff = False

    def endPose(self):
        motion_service  = self.session.service("ALMotion")
        posture_service = self.session.service("ALRobotPosture")
        # if self.isStiff:
        #     motion_service.setStiffnesses("Body", 0.0)
        # posture_service.goToPosture("Crouch", 0.5)
        # motion_service.setStiffnesses("Body", 1.0)
        motion_service.rest()
        self.isStiff = True

    def goToSitPose(self):
        posture_service = self.session.service("ALRobotPosture")
        posture_service.goToPosture("SitOnChair", 0.5)

    def goToPose(self, pose, blocking = False):
        posture_service = self.session.service("ALRobotPosture")
        if pose in posture_service.getPostureList:
            if blocking:
                posture_service.goToPosture(pose, 0.5)
            else:
                posture_service.post.goToPosture(pose, 0.5)

    def sendJoints(self, jointAngles, jointNames, jointTimes):
        motion_service  = self.session.service("ALMotion")
        model_service  = self.session.service("ALRobotModel")
        motion_service.setCollisionProtectionEnabled("Arms", True)
        # if self.isStiff:
        #     posture_service = self.session.service("ALRobotPosture")
        #     motion_service.setStiffnesses("Body", 0.0)
        #     posture_service.goToPosture("StandInit", 0.5)
        #     self.isStiff = False
        isAbsolute = True # kindoff is deprecated, but makes the joint positions absolute and not relative
        #motion_service.angleInterpolation(jointNames, jointAngles, jointTimes, isAbsolute) #the function talks with the robot
        #motion_service.angleInterpolationWithSpeed(jointNames, jointAngles, jointTimes, isAbsolute, _async) #the function talks with the robot
        motion_service.setAngles(jointNames, jointAngles, jointTimes)
    
    def initEndPose(self):
        motion_service  = self.session.service("ALMotion")
        model_service  = self.session.service("ALRobotModel")
        posture_service = self.session.service("ALRobotPosture")
        if self.isStiff and posture_service.getPosture() == "Crouching":
            #posture_service = self.session.service("ALRobotPosture")
            #print posture_service.getPosture()
            motion_service.setStiffnesses("Body", 0.0)
            #print posture_service.getPostureList()
            #posture_service.goToPosture("SitOnChair", 0.7)
            posture_service.goToPosture("StandInit", 0.7)
            self.isStiff = False
            #names = ["LHipPitch", "RHipPitch","LKneePitch","RKneePitch"]
            #angles = [-50.0*math.pi/180.0, -50.0*math.pi/180.0, 60.0*math.pi/180.0, 60.0*math.pi/180.0]
            #motion_service.setAngles(names, angles, 0.4)
        else:
            posture_service = self.session.service("ALRobotPosture")
            #posture_service.goToPosture("SitOnChair", 0.7)
            posture_service.goToPosture("StandInit", 0.7)
            motion_service.setStiffnesses("Body", 1.0)
            self.isStiff = True

    
    def play_sound(self, index, audio_file, unload_after_playing=True):
        # type: (int, str, bool) -> NaoPlaySoundAction
        if not os.path.isfile(audio_file):
            self.logger.info("Audio file does not exist", audio_file)
            print "Audio file does not exist"
            return
        audio_player = self.session.service("ALAudioPlayer")
        audio_device = self.session.service("ALAudioDevice")
        audio_device.muteAudioOut(False)
        audio_device.setOutputVolume(100)
        audio_player.setMasterVolume(1.0)
        preloaded_file = audio_player.loadFile(audio_file)
        audio_player.post.play(preloaded_file)
        audio_device.muteAudioOut(True)
        if unload_after_playing:
            audio_player.unloadFile(preloaded_file)

    def say(self, text):
        # type: (str) -> Say
        tts = self.session.service("ALTextToSpeech")
        audio_device = self.session.service("ALAudioDevice")
        audio_device.muteAudioOut(False)
        audio_device.setOutputVolume(100)
        tts.post.say(text)
        audio_device.muteAudioOut(True)
        #return Say(self.session, self.audio_atomic_counter, text)

    def startBehavior(self, behavior_name):
        """
        Launch a behavior, if possible.
        """
        behavior_mng_service = self.session.service("ALBehaviorManager")
        names = behavior_mng_service.getRunningBehaviors()
        print "Running behaviors:"
        print names
        # Check that the behavior exists.
        if (behavior_mng_service.isBehaviorInstalled(behavior_name)):
            # Check that it is not already running.
            if (not behavior_mng_service.isBehaviorRunning(behavior_name)):
                # Launch behavior. This is a blocking call, use _async=True if you do not
                # want to wait for the behavior to finish.
                behavior_mng_service.runBehavior(behavior_name, _async=True)
                self.startedBehavior = True
                #time.sleep(0.5)
            else:
                print "Behavior is already running."
        else:
            print "Behavior not found."

    def isBehaviorRunning(self, behavior_name):
        """
        check if a behavior exists and is running.
        """
        behavior_mng_service = self.session.service("ALBehaviorManager")
        # names = behavior_mng_service.getRunningBehaviors()
        # print "Running behaviors:"
        # print names
        # Check that the behavior exists.
        if (behavior_mng_service.isBehaviorInstalled(behavior_name)):
            # Check that it is not already running.
            if (behavior_mng_service.isBehaviorRunning(behavior_name)):
                return True
        return False

    def stopSystem(self):
        audio_device = self.session.service("ALAudioDevice")
        audio_device.muteAudioOut(True)
        self.udp_client.close_client()
        self.app.stop()

    def run(self):
        """
        Loop on, wait for events until manual interruption.
        """
        print "Starting Mirror Game"
        self.logger.info("Start")
        try:
            while True:
                if self.udp_client.have_new_data():
                    self.packetData = self.udp_client.get_json_packet()
                    self.audio_handler.parseNewPacket(self.packetData)
                    self.motion_handler.parseNewPacket(self.packetData)
                    if self.packetData["topic"] == self.LogTopic:
                        self.logger.info(self.packetData["logmsg"])
                else:
                    time.sleep(1)
        except KeyboardInterrupt:
            print "Interrupted by user, stopping Mirror Game"
        except Exception as e:
            print "Caught {0}: {1}".format(type(e), e)
        finally:
            print "Shutting down"
            self.logger.info("Stop")
            #self.endPose()
            self.motion_handler.goToEndPose()
            self.audio_handler.onClose()
            self.udp_client.close_client()
            self.app.stop()
            #self.stopSystem()
            sys.exit(0)

# if system exits with error, have it reboot
# self.session().service("ALSystem")
# system.reboot()

#if __name__ == "__main__":
parser = argparse.ArgumentParser()
parser.add_argument("--ip", type=str, default="127.0.0.1",
                    help="Robot IP address. On robot or Local Naoqi: use '127.0.0.1'.")
parser.add_argument("--port", type=int, default=9559,
                    help="Naoqi port number")

args = parser.parse_args()
try:
    # Initialize qi framework.
    connection_url = "tcp://" + args.ip + ":" + str(args.port)
    app = qi.Application(["MirrorClient", "--qi-url=" + connection_url])
except RuntimeError:
    print ("Can't connect to Naoqi at ip \"" + args.ip + "\" on port " + str(args.port) +".\n"
            "Please check your script arguments. Run with -h option for help.")
    sys.exit(1)

#app.start()
#session = app.session
#posture_service = session.service("ALRobotPosture")
#print posture_service.getPostureFamilyList()
mirror_client = MirrorClient(app, 'defaults.cfg')
mirror_client.run()
#udp_client = UDPClient('169.254.11.67', 20001)
#while(True):
#    if udp_client.have_new_data():
#        print udp_client.packet