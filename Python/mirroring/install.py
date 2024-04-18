import os
import shutil
from subprocess import CalledProcessError, check_call
from os import makedirs, path, symlink
import tempfile
from typing import Dict, List, Any, Callable
import json



from mirroring.exceptions import (
    FileDoesNotExistError,
    JSONSyntaxError,
    UserCorrectibleError,
)



def _run_command(cmd):
    # type: (List[str]) -> None
    check_call(cmd)

def end_running_process(
    nao_address,
    # This argument is only used in tests to log the commands rather than run them.
    run_command=_run_command,
):
    try:
        nao_user_and_address = "nao@{}".format(nao_address)
        try:
            # Kill any programs running from the mirroring directory
            print("\nEnd any running processes on the Nao")
            run_command(["ssh", nao_user_and_address, "pkill", "-f", "mirror_game"])
        except CalledProcessError:
            # This command may return a non-zero exit code, which is OK!
            pass
    except CalledProcessError:
        raise UserCorrectibleError("Ending processes failed.")
    else:
        #shutil.rmtree(performance_temp_dir)
        print("\nAll done!")

def upload_audio(
    nao_address,
    audio_path = "",
    # This argument is only used in tests to log the commands rather than run them.
    run_command=_run_command,
):
    try:
        nao_user_and_address = "nao@{}".format(nao_address)
        if audio_path == "":
            audio_path = path.join(path.abspath(path.join(path.dirname(__file__), "../")),"audio")
        print("\nUpload audio separately")
        run_command(
            [
                "rsync",
                "--recursive",
                "--verbose",
                "--compress",
                "--copy-links",
                "--human-readable",
                "--delete",
                "--progress",
                audio_path,
                "{}:/home/nao/mirror_game".format(nao_user_and_address),
            ],
        )
    except CalledProcessError:
        raise UserCorrectibleError("Audio upload failed.")
    else:
        #shutil.rmtree(performance_temp_dir)
        print("\nAudio uploaded successfully!")

def reload_code(
    nao_address,
    # This argument is only used in tests to log the commands rather than run them.
    run_command=_run_command,
):
    try:
        nao_user_and_address = "nao@{}".format(nao_address)
        code_root = path.abspath(path.join(path.dirname(__file__), "../"))
        print("\nUpload code directory")
        run_command(
            [
                "rsync",
                "--recursive",
                "--verbose",
                "--compress",
                "--copy-links",
                "--human-readable",
                "--delete",
                "--filter=:- {}".format(
                    path.abspath(path.join(code_root, ".gitignore"))
                ),
                "--progress",
                path.join(code_root, "mirroring"),
                path.join(code_root, "bin"),
                path.join(code_root, "audio"),
                path.join(code_root, "behaviors"),
                path.join(code_root, "mirror_game.py"),
                path.join(code_root, "test_command.py"),
                path.join(code_root, "udp_send.py"),
                path.join(code_root, "defaults.cfg"),
                "{}:/home/nao/mirror_game".format(nao_user_and_address),
            ],
        )
    except CalledProcessError:
        raise UserCorrectibleError("Code re-upload failed.")
    else:
        #shutil.rmtree(performance_temp_dir)
        print("\nCode re-uploaded successfully!")


def install_on_robot(
    nao_address,
    audio_path = "",
    # This argument is only used in tests to log the commands rather than run them.
    run_command=_run_command,
):
    # type: (str, str, str, str, Callable[[List[str]], None]) -> str
    """
    Installs the performance on a Nao robot to /home/nao/comedian.
    This path is currently not configurable since two performances cannot coexist on a single robot,
    but could be made configurable in the future.
    Returns the temp dir for use in tests.
    """

    try:
        nao_user_and_address = "nao@{}".format(nao_address)
        try:
            # Kill any programs running from the mirroring directory
            print("\nEnd any running processes on the Nao")
            run_command(["ssh", nao_user_and_address, "pkill", "-f", "mirror_game"])
        except CalledProcessError:
            # This command may return a non-zero exit code, which is OK!
            pass

        code_root = path.abspath(path.join(path.dirname(__file__), "../"))

        # Need to create the output folder on Nao first.
        print("\nMake necessary directories")
        run_command(
            [
                "ssh",
                nao_user_and_address,
                "mkdir",
                "-p",
                "/home/nao/mirror_game/NaoLogs",
            ]
        )
        print(code_root)
        print("\nUpload code directory")
        run_command(
            [
                "rsync",
                "--recursive",
                "--verbose",
                "--compress",
                "--copy-links",
                "--human-readable",
                "--delete",
                "--filter=:- {}".format(
                    path.abspath(path.join(code_root, ".gitignore"))
                ),
                "--progress",
                path.join(code_root, "mirroring"),
                path.join(code_root, "bin"),
                path.join(code_root, "audio"),
                path.join(code_root, "behaviors"),
                path.join(code_root, "mirror_game.py"),
                path.join(code_root, "test_command.py"),
                path.join(code_root, "udp_send.py"),
                path.join(code_root, "defaults.cfg"),
                "{}:/home/nao/mirror_game".format(nao_user_and_address),
            ],
        )

        # if audio_path == "":
        #     audio_path = path.join(code_root,"audio")
        # # because we accidentally filtered out the audio files
        # print("\nUpload audio separately")
        # run_command(
        #     [
        #         "rsync",
        #         "--recursive",
        #         "--verbose",
        #         "--compress",
        #         "--copy-links",
        #         "--human-readable",
        #         "--delete",
        #         "--progress",
        #         audio_path,
        #         "{}:/home/nao/mirror_game".format(nao_user_and_address),
        #     ],
        # )

        # Note: Need to run python programs under a bash login shell (bash -l)
        # so $PYTHONPATH is set, which lets `import qi` work.

        # install the performance to autorun
        # print("\nAdd program to autorun\n")
        # run_command(
        #     [
        #         "ssh",
        #         nao_user_and_address,
        #         "bash",
        #         "-lc",
        #         "'/home/nao/mirror_game/bin/nao_add_autorun_entry.py /home/nao/mirror_game/mirror_game.py'",
        #     ]
        # )

        # check that the performance does not have any invalid behaviors
        # run_command(
        #     [
        #         "ssh",
        #         nao_user_and_address,
        #         "bash",
        #         "-lc",
        #         "'/home/nao/comedian/comedy_robot/robots/nao/sanity_check_performance.py /home/nao/comedian/performance/performance.json'",
        #     ]
        # )

        # print("\nStart up the mirror game...\n")
        # run_command(
        #     [
        #         "ssh",
        #         nao_user_and_address,
        #         "bash",
        #         "-lc",
        #         "'nohup /home/nao/mirror_game/mirror_game.py > /dev/null 2> /dev/null < /dev/null &'",
        #     ]
        # )

        print(
            "\n\nSuccess! Mirroring game is now loaded on the Nao, and will run on robot startup."
        )

        #return performance_temp_dir
    except CalledProcessError:
        raise UserCorrectibleError("Installation failed.")
    finally:
        #shutil.rmtree(performance_temp_dir)
        print("\nAll done!")
