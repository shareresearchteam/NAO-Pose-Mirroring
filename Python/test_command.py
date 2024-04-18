#!/usr/bin/env python

import qi
import time
import sys
import argparse
import os
from naoqi import ALProxy


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

app.start()
session = app.session
posture_service = session.service("ALRobotPosture")
motion_service  = session.service("ALMotion")
#print posture_service.getPostureFamilyList()
motion_service.setStiffnesses("Body", 0.0)
posture_service.goToPosture("LyingBack", 0.5)
#posture_service.applyPosture("StandInit", 0.5)

app.stop()