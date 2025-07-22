using NetworkTester.Models;

namespace NetworkTester.Services;

public interface INetworkResourceService
{
    Task<List<VirtualMachineInfo>> GetVirtualMachinesAsync(string resourceGroup);
    Task<VirtualMachineInfo?> GetVirtualMachineAsync(string resourceGroup, string vmName);
    Task<string?> ResolveVMResourceIdAsync(string vmNameOrId);
    Task<List<VirtualNetworkInfo>> GetVirtualNetworksAsync(string resourceGroup);
    Task<List<SubnetInfo>> GetSubnetsAsync(string resourceGroup, string virtualNetworkName);
    Task<List<string>> GetNetworkSecurityGroupsAsync(string resourceGroup);
    Task<string?> GetNetworkInterfaceIdForVMAsync(string vmResourceId);
} 