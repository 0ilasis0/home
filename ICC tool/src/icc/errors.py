class ProfileError(Exception): pass
class InvalidProfileError(ProfileError): pass
class InvalidVcgtError(ProfileError): pass
class UnsupportedVcgtError(ProfileError): pass

class InvalidVcgtTxtError(ProfileError):
    """Raised when the TXT vcgt payload is malformed or invalid."""
    pass

class ProfileWriteError(ProfileError):
    """Raised when an error occurs while writing or modifying an ICC profile."""
    pass