#!/usr/bin/env python

from six.moves.configparser import RawConfigParser
from os import path

from mirroring.exceptions import FileDoesNotExistError


def add_autorun_entry(
    program_path, autoload_path="/home/nao/naoqi/preferences/autoload.ini"
):
    # type: (str, str) -> None
    if not path.exists(program_path):
        raise FileDoesNotExistError(program_path)

    if not path.exists(autoload_path):
        raise FileDoesNotExistError(autoload_path)

    config = RawConfigParser(allow_no_value=True)
    # Preserve case sensitivities in options. Otherwise, this will lowercase uppercase letters in the path!
    config.optionxform = str  # type: ignore
    config.read(autoload_path)
    if not config.has_section("program"):
        config.add_section("program")

    config.set("program", program_path)

    with open(autoload_path, "w") as autoload_file:
        config.write(autoload_file)
