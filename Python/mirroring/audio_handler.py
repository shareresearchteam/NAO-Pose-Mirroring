import qi
import os
import json
import datetime
import logging
from naoqi import ALProxy

class AudioHandler(object):
    """
    The class that handles audio logic pieces
    """
    
    def __init__(self, session, logger, audio_path):
        """
        Initialize with the qi framework connection, and the higher level logger
        """
        self.logger = logger
        self.session = session
        self.audioPath = audio_path

        self.audio_player = self.session.service("ALAudioPlayer")
        self.audio_device = self.session.service("ALAudioDevice")
        self.tts = self.session.service("ALTextToSpeech")

    def parseNewPacket(self, packet):
        if packet["topic"] == "Audio":
            audioName = packet["audiostring"]
            isAsync = packet["audioasync"]
            self.logger.info("PlayAudio, %s", audioName)
            self.startPlaySound(os.path.join(self.audioPath, audioName), isAsync)
        elif packet["topic"] == "Say":
            speakText = packet["audiostring"]
            self.logger.info("Say, %s", speakText)
            self.say(speakText)

    def enableAudio(self):
            self.audio_device.muteAudioOut(False)
            self.audio_device.setOutputVolume(100)
            self.audio_player.setMasterVolume(1.0)
    
    def startPlaySound(self, audio_file, isAsync):
        if not os.path.isfile(audio_file):
            self.logger.info("Audio file does not exist", audio_file)
            print "Audio file does not exist"
            return
        self.audio_player.unloadAllFiles()
        self.enableAudio()
        preloaded_file = self.audio_player.loadFile(audio_file)
        self.audio_player.play(preloaded_file, _async = isAsync)

    def onClose(self):
        self.audio_device.muteAudioOut(True)
        self.audio_player.unloadAllFiles()

    def say(self, text):
        self.enableAudio()
        self.tts.say(text, _async = True)
