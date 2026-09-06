using System.Net;
using System.Net.Sockets;

namespace Loren.Tools.Web;

internal static class PublicWebUrlPolicy
{
    public static bool TryNormalize(
        string? value,
        int maxCharacters,
        out Uri? normalizedUri)
    {
        normalizedUri = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string candidate = value.Trim();
        if (candidate.Length > maxCharacters
            || !Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri)
            || uri.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        if (!uri.IsDefaultPort
            && !((uri.Scheme == "http" && uri.Port == 80)
                || (uri.Scheme == "https" && uri.Port == 443)))
        {
            return false;
        }

        string host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (host.Length == 0
            || host == "localhost"
            || host.EndsWith(".localhost", StringComparison.Ordinal)
            || host.EndsWith(".local", StringComparison.Ordinal)
            || host.EndsWith(".internal", StringComparison.Ordinal))
        {
            return false;
        }

        if (IPAddress.TryParse(host, out IPAddress? address)
            && !IsPublicAddress(address))
        {
            return false;
        }

        normalizedUri = uri;
        return true;
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.None)
            || address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIpv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicIpv6(address.GetAddressBytes()),
            _ => false,
        };
    }

    private static bool IsPublicIpv4(byte[] bytes)
    {
        byte first = bytes[0];
        byte second = bytes[1];

        if (first is 0 or 10 or 127
            || first >= 224
            || (first == 100 && second is >= 64 and <= 127)
            || (first == 169 && second == 254)
            || (first == 172 && second is >= 16 and <= 31)
            || (first == 192 && second == 168)
            || (first == 198 && second is 18 or 19))
        {
            return false;
        }

        return true;
    }

    private static bool IsPublicIpv6(byte[] bytes)
    {
        if ((bytes[0] & 0xfe) == 0xfc)
        {
            return false;
        }

        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
        {
            return false;
        }

        if (bytes[0] == 0xff)
        {
            return false;
        }

        if (bytes[0] == 0x20
            && bytes[1] == 0x01
            && bytes[2] == 0x0d
            && bytes[3] == 0xb8)
        {
            return false;
        }

        return true;
    }
}
