#!/usr/bin/env python

from argparse import ArgumentParser
from os import path
import sys

# Ensure that comedy_robot imports work!
mirroring_dir = path.abspath(path.join(path.dirname(__file__), "../"))
sys.path.insert(0, mirroring_dir)

from mirroring.utilities.autorun import add_autorun_entry
from mirroring.exceptions import UserCorrectibleError

parser = ArgumentParser(
    description="Configures the Nao robot to autorun the given program at startup. Must be run on the robot."
)
parser.add_argument(
    "program",
    metavar="path",
    type=str,
    help="the path to the program to autorun",
)
args = parser.parse_args()

program_path = path.abspath(args.program)

try:
    add_autorun_entry(program_path)
    print("Nao will now autorun '{}' at startup.".format(program_path))
except UserCorrectibleError as e:
    sys.stderr.write(str(e) + "\n")
    exit(1)
