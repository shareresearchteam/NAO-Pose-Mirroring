from typing import List
import json
from jsonschema import ValidationError


class UserCorrectibleError(Exception):
    """
    Exceptions of this type are caused by user error, and can be corrected by the user.
    They should have a message that is easy for the user to understand.
    Command line interfaces should catch these exceptions and print their messages
    without a stack trace.
    """

    pass

    def closest_matches(self, original, possibilities, n=1):
        # type: (str, List[str], int) -> List[str]
        """
        Given a string and a list of possibilities, returns the n closest matches to original.
        """
        import pylev

        # Copy the input list so we don't mutate it under the caller.
        possibilities = list(possibilities)
        possibilities.sort(key=lambda x: pylev.levenshtein(original, x))
        return possibilities[0:n]


class FileDoesNotExistError(UserCorrectibleError):
    """
    Thrown when a particular file path does not exist.
    """

    def __init__(self, path):
        # type: (str) -> None
        self.path = path
        super(FileDoesNotExistError, self).__init__(
            "File '{}' does not exist".format(self.path)
        )


class JSONSyntaxError(UserCorrectibleError):
    def __init__(self, path, original_exception):
        # type: (str, json.JSONDecodeError) -> None
        self.path = path
        self.original_exception = original_exception
        super(JSONSyntaxError, self).__init__(
            "File '{}' has invalid JSON syntax:\n{}".format(
                self.path, self.original_exception.msg
            )
        )


class PerformanceJSONValidationError(UserCorrectibleError):
    def __init__(self, path, original_exception):
        # type: (str, ValidationError) -> None
        self.path = path
        self.original_exception = original_exception
        super(PerformanceJSONValidationError, self).__init__(
            "Performance '{}' failed validation against the JSON schema:\n{}".format(
                self.path, self.original_exception.message
            )
        )


class StopRoutineException(Exception):
    """
    Thrown when NAO should quickly abort the currently playing routine.
    This exception should always be caught.
    """

    def __init__(self):
        super(StopRoutineException, self).__init__(
            "Internal error: This exception should never be seen by a user."
        )

    pass


class InternalError(Exception):
    """
    Thrown when an internal error happens that is not user correctible.
    When this happens, there is a bug in the software!
    """

    def __init__(self, message):
        # type: (str) -> None
        super(InternalError, self).__init__("Internal error: {}".format(message))
