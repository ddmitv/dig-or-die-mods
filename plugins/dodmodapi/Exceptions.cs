
using System;

namespace DODModAPI;

public sealed class LateRegistrationException : InvalidOperationException {
    public LateRegistrationException()
        : base("Late registration is not allowed. The initialization phase has already completed. Please register custom content in your plugin's Awake() method.") {}

    public LateRegistrationException(string message)
        : base(message) {}

    public static void ThrowIfLocked(bool isLocked) {
        if (isLocked) {
            throw new LateRegistrationException();
        }
    }
}
