namespace NetworkTester.Models;

public class VirtualMachineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string PowerState { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string PrivateIpAddress { get; set; } = string.Empty;
    public string? PublicIpAddress { get; set; }
    public string SubnetName { get; set; } = string.Empty;
    public string VirtualNetworkName { get; set; } = string.Empty;
    public List<string> NetworkSecurityGroups { get; set; } = new();
    public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime CreatedTime { get; set; }
}

public class NetworkInterfaceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PrivateIpAddress { get; set; } = string.Empty;
    public string? PublicIpAddress { get; set; }
    public bool IsPrimary { get; set; }
    public string SubnetId { get; set; } = string.Empty;
    public List<string> SecurityGroupIds { get; set; } = new();
}

public class SubnetInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AddressPrefix { get; set; } = string.Empty;
    public string VirtualNetworkName { get; set; } = string.Empty;
    public List<string> ConnectedDevices { get; set; } = new();
    public string? NetworkSecurityGroupId { get; set; }
    public string? RouteTableId { get; set; }
}

public class VirtualNetworkInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> AddressSpaces { get; set; } = new();
    public List<SubnetInfo> Subnets { get; set; } = new();
    public List<VirtualNetworkPeering> Peerings { get; set; } = new();
    public bool DdosProtectionEnabled { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class VirtualNetworkPeering
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RemoteVirtualNetworkId { get; set; } = string.Empty;
    public string PeeringState { get; set; } = string.Empty;
    public bool AllowVirtualNetworkAccess { get; set; }
    public bool AllowForwardedTraffic { get; set; }
    public bool AllowGatewayTransit { get; set; }
    public bool UseRemoteGateways { get; set; }
} 