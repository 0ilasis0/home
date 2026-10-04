class ProfileError(Exception): pass
class InvalidProfileError(ProfileError): pass

class InvalidVcgtError(ProfileError):
    """Raised when the vcgt payload is malformed."""
    pass

class UnsupportedVcgtError(ProfileError):
    """Raised when the vcgt payload uses unsupported features (e.g., Formula Type)."""
    pass