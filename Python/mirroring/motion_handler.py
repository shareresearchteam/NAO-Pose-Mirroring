import qi
import json
import datetime
import logging
from naoqi import ALProxy

class MotionHandler(object):
    """
    The class that handles motion logic pieces
    """
    
    def __init__(self, session, logger):
        """
        Initialize with the qi framework connection, and the higher level logger
        """
        self.logger = logger
        self.session = session
        self.behavior_mng_service = self.session.service("ALBehaviorManager")
        self.posture_service = self.session.service("ALRobotPosture")
        self.motion_service  = self.session.service("ALMotion")
        self.model_service  = self.session.service("ALRobotModel")
        self.motion_service.setCollisionProtectionEnabled("Arms", True)

        # Setup our "constants" that we might want to change later


        # Setup our working values
        self.currentMode = -1
        self.startNewMode = False
        self.currentBehavior = "none"
        self.followParticipant = 0

        # Set up our signal connections
        self.behavior_mng_service.behaviorStopped.connect(self.onBehaviorStopped)

    def parseNewPacket(self, packet):
        if packet["topic"] == "Mode":
            newMode = int(packet["mode"])
            if newMode == 0:
                self.currentMode = 0
                self.goToInitPose()
                #self.motion_service.setIdlePostureEnabled("Body", False)
            elif newMode == 1:
                self.currentMode = 1
                #self.motion_service.setIdlePostureEnabled("Body", True)
                new_action = packet["behavior"]
                self.logger.info("Lead, %s", new_action)
                print "Lead, {}".format(new_action)
                self.startBehavior(new_action)
            elif newMode == 2:
                if self.currentMode == 2:
                    self.followParticipant += 1
                else:
                    self.followParticipant = 0
                self.logger.info("Follow, %s", str(self.followParticipant))
                print "Follow, {}".format(self.followParticipant)
                self.currentMode = 2
                #self.motion_service.setIdlePostureEnabled("Body", False)
            else:
                self.currentMode = -1
                self.goToEndPose()
        elif packet["topic"] == "Joints" and self.currentMode == 2:
            jointAngleList = packet["anglelist"]
            angleNames = jointAngleList["jointnames"]
            angleList = jointAngleList["jointangles"]
            jointTimes = jointAngleList["jointtimes"]
            for i in angleList:
                if i == "NaN" or i == float('nan'):
                    return
            #print "Joint Names: {}".format(angleNames)
            #print "Joint Angles: {}".format(angleList)
            #print "Joint Times: {}".format(jointTimes)
            self.logger.info("FollowJoints, %s", jointAngleList)
            self.sendJoints(angleList, angleNames, jointTimes)

    def goToInitPose(self):
        self.motion_service.setSmartStiffnessEnabled(True)
        self.motion_service.wakeUp()

    def goToEndPose(self):
        self.motion_service.rest()

    def goToNeutralPose(self):
        self.posture_service.goToPosture("Stand", 0.5, _async=True)
        #self.motion_service.wakeUp()

    def onBehaviorStopped(self, behavior_name):
        if behavior_name == self.currentBehavior:
            # Reset pose to neutral position
            print "Stopped behavior: " + behavior_name +",   Current Behavior: " + self.currentBehavior
            self.goToNeutralPose()

    def startBehavior(self, behavior_name):
        # Check that the behavior exists.
        if (self.behavior_mng_service.isBehaviorInstalled(behavior_name)):
            # Check that it is not already running.
            if (not self.behavior_mng_service.isBehaviorRunning(behavior_name)):
                self.currentBehavior = behavior_name
                self.behavior_mng_service.runBehavior(behavior_name, _async=True)
            else:
                print "Behavior is already running."
        else:
            # TODO: maybe try to find the behavior?
            print "Behavior does not exist."


    def canRunBehavior(self, behavior_name):
        """
        check if we can safely run a behavior.
        """
        # Check that the behavior exists.
        if (self.behavior_mng_service.isBehaviorInstalled(behavior_name)):
            # Check that it is not already running.
            if (not self.behavior_mng_service.isBehaviorRunning(behavior_name)):
                return True
        return False
            
    def sendJoints(self, jointAngles, jointNames, jointTimes):
        # if self.isStiff:
        #     posture_service = self.session.service("ALRobotPosture")
        #     motion_service.setStiffnesses("Body", 0.0)
        #     posture_service.goToPosture("StandInit", 0.5)
        #     self.isStiff = False
        isAbsolute = True # kindoff is deprecated, but makes the joint positions absolute and not relative
        # This interpolates and angle path over the time in joint times, is absolute is absolute angles, try with post, otherwise try with _async
        #self.motion_service.angleInterpolation(jointNames, jointAngles, jointTimes, isAbsolute, _async = True) 
        #motion_service.angleInterpolationWithSpeed(jointNames, jointAngles, jointTimes, isAbsolute, _async) #the function talks with the robot
        self.motion_service.setAngles(jointNames, jointAngles, jointTimes) # this one works, but it's rough
            