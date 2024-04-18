#!/usr/bin/env python3

from argparse import ArgumentParser
from os import path
import sys

# Ensure that comedy_robot imports work!
root_dir = path.abspath(path.join(path.dirname(__file__), "../"))
sys.path.insert(0, root_dir)

from mirroring.exceptions import UserCorrectibleError
from mirroring.install import end_running_process, upload_audio, reload_code, install_on_robot

parser = ArgumentParser(
    prog='MirrorGameLoader',
    description="Configures the Nao robot to perform the given performance on startup."
)

parser.add_argument(
    "--nao",
    metavar="address",
    type=str,
    required=False,
    default="nao.local",
    help="address of the Nao robot to install the performance to",
)
parser.add_argument('--pkill', 
    type=bool, 
    default=False,
    help="Call to end running processes on the NAO, but not full install")

parser.add_argument('--reload',
    type=bool,
    default=False,
    help="Call to just reload the code, no directories are made, no processes are ended")

parser.add_argument('--audio', 
    type=bool, 
    default=False,
    help="Call when wanting to upload audio, but not run full install")

parser.add_argument('--audiopath', 
    metavar="path",
    type=str, 
    default=path.join(root_dir,"audio"),
    help="If path to the audio files is different from the default")
args = parser.parse_args()
is_audio_only = args.audio  # type: bool
audio_path = args.audiopath  # type: str
is_pkill_only = args.pkill  # type: bool
is_reload_only = args.reload  # type: bool
nao_address = args.nao

try:
    if is_pkill_only:
        end_running_process(nao_address)
    elif is_audio_only:
        upload_audio(nao_address, audio_path)
    elif is_reload_only:
        reload_code(nao_address)
    else:
        install_on_robot(
            nao_address, audio_path
        )
except UserCorrectibleError as e:
    sys.stderr.write(str(e) + "\n")
    exit(1)
