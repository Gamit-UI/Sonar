using System.Runtime.InteropServices;
using Sonar.Rules.Extensions;

namespace Sonar.Helpers;

internal static class DomainHelper
{
    private const int ErrorSuccess = 0;
    private const int ErrorNoMoreItems = 0x103;
    private const int ErrorFilemarkDetected = 0x44d;

    [Flags]
    public enum DsGetDcNameFlags : uint
    {
        /// <summary>
        /// Forces cached domain controller data to be ignored. When the DS_FORCE_REDISCOVERY flag is not specified, DsGetDcName may
        /// return cached domain controller data. If this flag is specified, DsGetDcName will not use cached information (if any exists)
        /// but will instead perform a fresh domain controller discovery.
        /// <para>
        /// This flag should not be used under normal conditions, as using the cached domain controller information has better
        /// performance characteristics and helps to ensure that the same domain controller is used consistently by all applications.
        /// This flag should be used only after the application determines that the domain controller returned by DsGetDcName (when
        /// called without this flag) is not accessible. In that case, the application should repeat the DsGetDcName call with this flag
        /// to ensure that the unuseful cached information (if any) is ignored and a reachable domain controller is discovered.
        /// </para>
        /// </summary>
        DsForceRediscovery = 0x1,

        /// <summary>Requires that the returned domain controller support directory services.</summary>
        DsDirectoryServiceRequired = 0x10,

        /// <summary>
        /// DsGetDcName attempts to find a domain controller that supports directory service functions. If a domain controller that
        /// supports directory services is not available, DsGetDcName returns the name of a non-directory service domain controller.
        /// However, DsGetDcName only returns a non-directory service domain controller after the attempt to find a directory service
        /// domain controller times out.
        /// </summary>
        DsDirectoryServicePreferred = 0x20,

        /// <summary>
        /// Requires that the returned domain controller be a global catalog server for the forest of domains with this domain as the
        /// root. If this flag is set and the DomainName parameter is not NULL, DomainName must specify a forest name. This flag cannot
        /// be combined with the DS_PDC_REQUIRED or DS_KDC_REQUIRED flags.
        /// </summary>
        DsGcServerRequired = 0x40,

        /// <summary>
        /// Requires that the returned domain controller be the primary domain controller for the domain. This flag cannot be combined
        /// with the DS_KDC_REQUIRED or DS_GC_SERVER_REQUIRED flags.
        /// </summary>
        DsPdcRequired = 0x80,

        /// <summary>
        /// If the DS_FORCE_REDISCOVERY flag is not specified, this function uses cached domain controller data. If the cached data is
        /// more than 15 minutes old, the cache is refreshed by pinging the domain controller. If this flag is specified, this refresh is
        /// avoided even if the cached data is expired. This flag should be used if the DsGetDcName function is called periodically.
        /// </summary>
        DsBackgroundOnly = 0x100,

        /// <summary>
        /// This parameter indicates that the domain controller must have an IP address. In that case, DsGetDcName will place the
        /// Internet protocol address of the domain controller in the DomainControllerAddress member of DomainControllerInfo.
        /// </summary>
        DsIpRequired = 0x200,

        /// <summary>
        /// Requires that the returned domain controller be currently running the Kerberos Key Distribution Center service. This flag
        /// cannot be combined with the DS_PDC_REQUIRED or DS_GC_SERVER_REQUIRED flags.
        /// </summary>
        DsKdcRequired = 0x400,

        /// <summary>Requires that the returned domain controller be currently running the Windows Time Service.</summary>
        DsTimeservRequired = 0x800,

        /// <summary>Requires that the returned domain controller be writable; that is, host a writable copy of the directory service.</summary>
        DsWritableRequired = 0x1000,

        /// <summary>
        /// DsGetDcName attempts to find a domain controller that is a reliable time server. The Windows Time Service can be configured
        /// to declare one or more domain controllers as a reliable time server. For more information, see the Windows Time Service
        /// documentation. This flag is intended to be used only by the Windows Time Service.
        /// </summary>
        DsGoodTimeservPreferred = 0x2000,

        /// <summary>
        /// When called from a domain controller, specifies that the returned domain controller name should not be the current computer.
        /// If the current computer is not a domain controller, this flag is ignored. This flag can be used to obtain the name of another
        /// domain controller in the domain.
        /// </summary>
        DsAvoidSelf = 0x4000,

        /// <summary>
        /// Specifies that the server returned is an LDAP server. The server returned is not necessarily a domain controller. No other
        /// services are implied to be present at the server. The server returned does not necessarily have a writable config container
        /// nor a writable schema container. The server returned may not necessarily be used to create or modify security principles.
        /// This flag may be used with the DS_GC_SERVER_REQUIRED flag to return an LDAP server that also hosts a global catalog server.
        /// The returned global catalog server is not necessarily a domain controller. No other services are implied to be present at the
        /// server. If this flag is specified, the DS_PDC_REQUIRED, DS_TIMESERV_REQUIRED, DS_GOOD_TIMESERV_PREFERRED,
        /// DS_DIRECTORY_SERVICES_PREFERED, DS_DIRECTORY_SERVICES_REQUIRED, and DS_KDC_REQUIRED flags are ignored.
        /// </summary>
        DsOnlyLdapNeeded = 0x8000,

        /// <summary>
        /// Specifies that the DomainName parameter is a flat name. This flag cannot be combined with the DS_IS_DNS_NAME flag.
        /// </summary>
        DsIsFlatName = 0x10000,

        /// <summary>
        /// Specifies that the DomainName parameter is a DNS name. This flag cannot be combined with the DS_IS_FLAT_NAME flag.
        /// <para>
        /// Specify either DS_IS_DNS_NAME or DS_IS_FLAT_NAME. If neither flag is specified, DsGetDcName may take longer to find a domain
        /// controller because it may have to search for both the DNS-style and flat name.
        /// </para>
        /// </summary>
        DsIsDnsName = 0x20000,

        /// <summary>
        /// When this flag is specified, DsGetDcName attempts to find a domain controller in the same site as the caller. If no such
        /// domain controller is found, it will find a domain controller that can provide topology information and call DsBindToISTG to
        /// obtain a bind handle, then call DsQuerySitesByCost over UDP to determine the "next closest site," and finally cache the name
        /// of the site found. If no domain controller is found in that site, then DsGetDcName falls back on the default method of
        /// locating a domain controller.
        /// <para>
        /// If this flag is used in conjunction with a non-NULL value in the input parameter SiteName, then ERROR_INVALID_FLAGS is thrown.
        /// </para>
        /// <para>
        /// Also, the kind of search employed with DS_TRY_NEXT_CLOSEST_SITE is site-specific, so this flag is ignored if it is used in
        /// conjunction with DS_PDC_REQUIRED. Finally, DS_TRY_NEXTCLOSEST_SITE is ignored when used in conjunction with
        /// DS_RETURN_FLAT_NAME because that uses NetBIOS to resolve the name, but the domain of the domain controller found won't
        /// necessarily match the domain to which the client is joined.
        /// </para>
        /// <note>Note This flag is Group Policy enabled. If you enable the "Next Closest Site" policy setting, Next Closest Site DC
        /// Location will be turned on for the machine across all available but un-configured network adapters. If you disable the policy
        /// setting, Next Closest Site DC Location will not be used by default for the machine across all available but un-configured
        /// network adapters. However, if a DC Locator call is made using the DS_TRY_NEXTCLOSEST_SITE flag explicitly, DsGetDcName honors
        /// the Next Closest Site behavior. If you do not configure this policy setting, Next Closest Site DC Location will be not be
        /// used by default for the machine across all available but un-configured network adapters. If the DS_TRY_NEXTCLOSEST_SITE flag
        /// is used explicitly, the Next Closest Site behavior will be used.</note>
        /// </summary>
        DsTryNextclosestSite = 0x40000,

        /// <summary>Requires that the returned domain controller be running Windows Server 2008 or later.</summary>
        DsDirectoryService6Required = 0x80000,

        /// <summary>Requires that the returned domain controller be currently running the Active Directory web service.</summary>
        DsWebServiceRequired = 0x100000,

        /// <summary>Requires that the returned domain controller be running Windows Server 2012 or later.</summary>
        DsDirectoryService8Required = 0x200000,

        /// <summary>Requires that the returned domain controller be running Windows Server 2012 R2 or later.</summary>
        DsDirectoryService9Required = 0x400000,

        /// <summary>Requires that the returned domain controller be running Windows Server 2016 or later.</summary>
        DsDirectoryService10Required = 0x800000,

        /// <summary>
        /// Specifies that the names returned in the DomainControllerName and DomainName members of DomainControllerInfo should be DNS
        /// names. If a DNS name is not available, an error is returned. This flag cannot be specified with the DS_RETURN_FLAT_NAME flag.
        /// This flag implies the DS_IP_REQUIRED flag.
        /// </summary>
        DsReturnDnsName = 0x40000000,

        /// <summary>
        /// Specifies that the names returned in the DomainControllerName and DomainName members of DomainControllerInfo should be flat
        /// names. If a flat name is not available, an error is returned. This flag cannot be specified with the DS_RETURN_DNS_NAME flag.
        /// </summary>
        DsReturnFlatName = 0x80000000,
    }

    [Flags]
    public enum DsGetDcOpenOptions
    {
        /// <summary>Only site-specific domain controllers are enumerated.</summary>
        DsOnlyDoSiteName = 0x01,

        /// <summary>
        /// The DsGetDcNext function will return the ERROR_FILEMARK_DETECTED value after all of the site-specific domain controllers are
        /// retrieved. DsGetDcNext will then enumerate the second group, which contains all domain controllers in the domain, including
        /// the site-specific domain controllers contained in the first group.
        /// </summary>
        DsNotifyAfterSiteRecords = 0x02,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DomainControllerInfo
    {
        public string DomainControllerName;
        public string DomainControllerAddress;
        public int DomainControllerAddressType;
        public Guid DomainGuid;
        public string DomainName;
        public string DnsForestName;
        public int Flags;
        public string DcSiteName;
        public string ClientSiteName;
    }

    [DllImport("Netapi32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    public static extern int DsGetDcName([Optional] string? computerName, [Optional] string? domainName, in Guid domainGuid, [Optional] string? siteName, DsGetDcNameFlags flags, out IntPtr domainControllerInfo);

    [DllImport("Netapi32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    public static extern int DsGetDcOpen(string dnsName, DsGetDcOpenOptions optionFlags, [Optional] string? siteName, in Guid domainGuid, [Optional] string? dnsForestName, DsGetDcNameFlags dcFlags, out IntPtr retGetDcContext);

    [DllImport("Netapi32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    public static extern int DsGetDcNext(IntPtr getDcContextHandle, out uint sockAddressCount, out IntPtr sockAddresses, out IntPtr dnsHostName);

    [DllImport("Netapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void DsGetDcClose(IntPtr getDcContext);

    [DllImport("Netapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int NetApiBufferFree(IntPtr buffer);

    public static IEnumerable<string> EnumerateDomainControllers()
    {
        var dwRet = DsGetDcName(null, domainName: null, Guid.Empty, siteName: null, flags: 0, out var pDcInfo);
        if (dwRet == ErrorSuccess)
        {
            var dcInfo = Marshal.PtrToStructure<DomainControllerInfo>(pDcInfo);
            var domainName = dcInfo.DomainName;
            yield return dcInfo.DomainControllerName.Replace("\\", string.Empty).StripDomain();
            dwRet = DsGetDcOpen(domainName, optionFlags: DsGetDcOpenOptions.DsNotifyAfterSiteRecords, siteName: null, dcInfo.DomainGuid, dnsForestName: null, dcFlags: 0, out var hGetDcContext);
            if (dwRet == ErrorSuccess)
            {
                while (true)
                {
                    dwRet = DsGetDcNext(hGetDcContext, out _, out var pSocketAddresses, out var pDnsHostName);
                    if (dwRet == ErrorSuccess)
                    {
                        string? hostname = Marshal.PtrToStringAuto(pDnsHostName);
                        if (!string.IsNullOrWhiteSpace(hostname))
                        {
                            yield return hostname.Replace("\\", string.Empty).StripDomain();
                        }

                        NetApiBufferFree(pDnsHostName);
                        NetApiBufferFree(pSocketAddresses);
                    }
                    else if (dwRet == ErrorNoMoreItems)
                    {
                        break;
                    }
                    else if (dwRet == ErrorFilemarkDetected)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                DsGetDcClose(hGetDcContext);
            }

            NetApiBufferFree(pDcInfo);
        }
    }
}