using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Reverie.Services;

public class LockService
{
    private const string PinKey = "reverie.pin";
    private const string PinEnabledKey = "pin_enabled";
    private bool _isUnlocked = false; // ADD THIS LINE

    public int PinLength => 4;

    // Check if PIN is enabled
    public async Task<bool> IsPinEnabledAsync()
    {
        var enabled = await SecureStorage.GetAsync(PinEnabledKey);
        return enabled == "true";
    }

    // Check if PIN exists
    public async Task<bool> HasPinAsync()
    {
        var pin = await SecureStorage.GetAsync(PinKey);
        return !string.IsNullOrWhiteSpace(pin);
    }

    // Set PIN and enable protection
    public async Task SetPinAsync(string pin)
    {
        if (pin.Length != 4 || !pin.All(char.IsDigit))
            throw new Exception("PIN must be 4 digits.");

        await SecureStorage.SetAsync(PinKey, pin);
        await SecureStorage.SetAsync(PinEnabledKey, "true");
        _isUnlocked = true;
    }

    // Clear PIN and disable protection
    public async Task ClearPinAsync()
    {
        SecureStorage.Remove(PinKey);
        await SecureStorage.SetAsync(PinEnabledKey, "false");
        _isUnlocked = true;
    }

    // Verify PIN
    public async Task<bool> VerifyPinAsync(string pin)
    {
        var saved = await SecureStorage.GetAsync(PinKey);
        var isCorrect = saved == pin;

        if (isCorrect)
        {
            _isUnlocked = true;
        }

        return isCorrect;
    }

    // Enable/disable PIN without setting
    public async Task SetPinEnabledAsync(bool enabled)
    {
        if (enabled)
        {
            await SecureStorage.SetAsync(PinEnabledKey, "true");
        }
        else
        {
            await SecureStorage.SetAsync(PinEnabledKey, "false");
            SecureStorage.Remove(PinKey);
            _isUnlocked = true;
        }
    }

    // ADD THIS METHOD
    public bool IsUnlockedThisSession()
    {
        return _isUnlocked;
    }

    // Lock the app
    public void Lock()
    {
        _isUnlocked = false;
    }
}