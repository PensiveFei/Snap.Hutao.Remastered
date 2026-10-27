// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;

namespace Snap.Hutao.Remastered.Core.LifeCycle;

public sealed class HutaoActivationArguments
{
    public bool IsRedirectTo { get; set; }

    public HutaoActivationKind Kind { get; set; }

    public Uri? ProtocolActivatedUri { get; set; }

    public string? LaunchActivatedArguments { get; set; }

    public IReadOnlyDictionary<string, string>? AppNotificationActivatedArguments { get; set; }

    public IReadOnlyDictionary<string, string>? AppNotificationActivatedUserInput { get; set; }

    public static HutaoActivationArguments FromAppActivationArguments(AppActivationArguments args, bool isRedirected = false)
    {
        HutaoActivationArguments result = new()
        {
            IsRedirectTo = isRedirected,
        };

        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                {
                    result.Kind = HutaoActivationKind.Launch;
                    if (args.TryGetLaunchActivatedArguments(out string? arguments))
                    {
                        result.LaunchActivatedArguments = arguments;
                    }

                    break;
                }

            case ExtendedActivationKind.Protocol:
                {
                    result.Kind = HutaoActivationKind.Protocol;
                    if (args.TryGetProtocolActivatedUri(out Uri? uri))
                    {
                        result.ProtocolActivatedUri = uri;
                    }

                    break;
                }

            case ExtendedActivationKind.AppNotification:
                {
                    result.Kind = HutaoActivationKind.AppNotification;
                    if (args.TryGetAppNotificationActivatedArguments(out string? argument, out IDictionary<string, string>? arguments, out IDictionary<string, string>? userInput))
                    {
                        result.LaunchActivatedArguments = argument;
                        result.AppNotificationActivatedArguments = arguments.AsReadOnly();
                        result.AppNotificationActivatedUserInput = userInput.AsReadOnly();
                    }

                    break;
                }
        }

        return result;
    }

    /// <summary>
    /// Reads the activation carried by the command line. The administrator restart forwards the
    /// activation as an argument, see <c>Bootstrap.RestartAsAdministrator</c>.
    /// </summary>
    public static HutaoActivationArguments? TryCreateFromCommandLineArguments(IReadOnlyList<string> args)
    {
        if (args.Count is 0)
        {
            return null;
        }

        string? argument = args[0];
        if (string.IsNullOrEmpty(argument))
        {
            return null;
        }

        if (argument.StartsWith("hutao://", StringComparison.OrdinalIgnoreCase))
        {
            return new()
            {
                Kind = HutaoActivationKind.Protocol,
                ProtocolActivatedUri = new(argument),
            };
        }

        return new()
        {
            Kind = HutaoActivationKind.Launch,
            LaunchActivatedArguments = argument,
        };
    }

    /// <summary>
    /// Restores the protocol activation that the administrator restart forwards through the command
    /// line. Without it the app model reports the forwarded <c>hutao://</c> argument as a plain
    /// launch, which only brings the main window up instead of starting the game.
    /// </summary>
    public static HutaoActivationArguments ResolveFromAppActivationArguments(AppActivationArguments args, bool isRedirected = false)
    {
        HutaoActivationArguments result = FromAppActivationArguments(args, isRedirected);

        if (result.Kind is not HutaoActivationKind.Launch)
        {
            return result;
        }

        if (TryCreateFromCommandLineArguments([result.LaunchActivatedArguments ?? string.Empty]) is not { Kind: HutaoActivationKind.Protocol } forwarded)
        {
            return result;
        }

        forwarded.IsRedirectTo = isRedirected;
        return forwarded;
    }
}