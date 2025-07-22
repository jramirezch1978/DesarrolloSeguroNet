using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkTester.Models;

namespace NetworkTester.Services;

public class NetworkResourceService : INetworkResourceService
{
    private readonly ILogger<NetworkResourceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ArmClient _armClient;

    public NetworkResourceService(ILogger<NetworkResourceService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _armClient = new ArmClient(new DefaultAzureCredential());
    }

    public async Task<List<VirtualMachineInfo>> GetVirtualMachinesAsync(string resourceGroup)
    {
        try
        {
            _logger.LogDebug($"Obteniendo VMs del resource group: {resourceGroup}");
            
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroup);
            
            var vms = new List<VirtualMachineInfo>();
            
            await foreach (var vm in resourceGroupResource.Value.GetVirtualMachines())
            {
                var vmInfo = await ConvertToVMInfoAsync(vm);
                vms.Add(vmInfo);
            }
            
            _logger.LogInformation($"Encontradas {vms.Count} VMs en {resourceGroup}");
            return vms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo VMs del resource group {resourceGroup}");
            return new List<VirtualMachineInfo>();
        }
    }

    public async Task<VirtualMachineInfo?> GetVirtualMachineAsync(string resourceGroup, string vmName)
    {
        try
        {
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroup);
            var vm = await resourceGroupResource.Value.GetVirtualMachineAsync(vmName);
            
            return await ConvertToVMInfoAsync(vm.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo VM {vmName} del resource group {resourceGroup}");
            return null;
        }
    }

    public async Task<string?> ResolveVMResourceIdAsync(string vmNameOrId)
    {
        try
        {
            // Si ya es un resource ID, devolverlo
            if (vmNameOrId.StartsWith("/subscriptions/"))
            {
                return vmNameOrId;
            }

            // Buscar en todos los resource groups (para simplificar el laboratorio)
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            
            await foreach (var resourceGroup in subscription.GetResourceGroups())
            {
                try
                {
                    var vm = await resourceGroup.GetVirtualMachineAsync(vmNameOrId);
                    return vm.Value.Id.ToString();
                }
                catch
                {
                    // VM no encontrada en este resource group, continuar
                    continue;
                }
            }

            _logger.LogWarning($"No se pudo resolver la VM: {vmNameOrId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error resolviendo VM {vmNameOrId}");
            return null;
        }
    }

    public async Task<List<VirtualNetworkInfo>> GetVirtualNetworksAsync(string resourceGroup)
    {
        try
        {
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroup);
            
            var vnets = new List<VirtualNetworkInfo>();
            
            await foreach (var vnet in resourceGroupResource.Value.GetVirtualNetworks())
            {
                var vnetInfo = await ConvertToVNetInfoAsync(vnet);
                vnets.Add(vnetInfo);
            }
            
            return vnets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo VNets del resource group {resourceGroup}");
            return new List<VirtualNetworkInfo>();
        }
    }

    public async Task<List<SubnetInfo>> GetSubnetsAsync(string resourceGroup, string virtualNetworkName)
    {
        try
        {
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroup);
            var vnet = await resourceGroupResource.Value.GetVirtualNetworkAsync(virtualNetworkName);
            
            var subnets = new List<SubnetInfo>();
            
            await foreach (var subnet in vnet.Value.GetSubnets())
            {
                var subnetInfo = ConvertToSubnetInfo(subnet);
                subnets.Add(subnetInfo);
            }
            
            return subnets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo subnets de {virtualNetworkName} en {resourceGroup}");
            return new List<SubnetInfo>();
        }
    }

    public async Task<List<string>> GetNetworkSecurityGroupsAsync(string resourceGroup)
    {
        try
        {
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            var resourceGroupResource = await subscription.GetResourceGroupAsync(resourceGroup);
            
            var nsgs = new List<string>();
            
            await foreach (var nsg in resourceGroupResource.Value.GetNetworkSecurityGroups())
            {
                nsgs.Add(nsg.Data.Name);
            }
            
            return nsgs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo NSGs del resource group {resourceGroup}");
            return new List<string>();
        }
    }

    public async Task<string?> GetNetworkInterfaceIdForVMAsync(string vmResourceId)
    {
        try
        {
            var vmResource = _armClient.GetVirtualMachineResource(new Azure.Core.ResourceIdentifier(vmResourceId));
            var vm = await vmResource.GetAsync();
            
            // Obtener la primera NIC (primaria)
            var primaryNic = vm.Value.Data.NetworkProfile?.NetworkInterfaces?.FirstOrDefault(nic => nic.Primary == true)
                          ?? vm.Value.Data.NetworkProfile?.NetworkInterfaces?.FirstOrDefault();
            
            return primaryNic?.Id?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error obteniendo NIC ID para VM {vmResourceId}");
            return null;
        }
    }

    private async Task<VirtualMachineInfo> ConvertToVMInfoAsync(VirtualMachineResource vm)
    {
        var vmData = vm.Data;
        var instanceView = await vm.GetInstanceViewAsync();
        
        var vmInfo = new VirtualMachineInfo
        {
            Id = vm.Id.ToString(),
            Name = vmData.Name,
            ResourceGroup = vm.Id.ResourceGroupName ?? string.Empty,
            Location = vmData.Location.ToString(),
            Size = vmData.HardwareProfile?.VmSize?.ToString() ?? string.Empty,
            OperatingSystem = vmData.StorageProfile?.OsDisk?.OSType?.ToString() ?? "Unknown",
            Tags = vmData.Tags?.ToDictionary(t => t.Key, t => t.Value) ?? new Dictionary<string, string>()
        };

        // Obtener estado de power
        var powerState = instanceView.Value.Statuses?
            .FirstOrDefault(s => s.Code?.StartsWith("PowerState/") == true)?
            .Code?.Substring("PowerState/".Length) ?? "Unknown";
        vmInfo.PowerState = powerState;

        // Obtener información de red
        if (vmData.NetworkProfile?.NetworkInterfaces?.Any() == true)
        {
            foreach (var nicRef in vmData.NetworkProfile.NetworkInterfaces)
            {
                try
                {
                    var nicResource = _armClient.GetNetworkInterfaceResource(nicRef.Id);
                    var nic = await nicResource.GetAsync();
                    
                    var nicInfo = new NetworkInterfaceInfo
                    {
                        Id = nic.Value.Id.ToString(),
                        Name = nic.Value.Data.Name,
                        IsPrimary = nicRef.Primary ?? false
                    };

                    // Obtener IP privada
                    var ipConfig = nic.Value.Data.IPConfigurations?.FirstOrDefault();
                    if (ipConfig != null)
                    {
                        nicInfo.PrivateIpAddress = ipConfig.PrivateIPAddress ?? string.Empty;
                        nicInfo.SubnetId = ipConfig.Subnet?.Id?.ToString() ?? string.Empty;
                        
                        // Establecer IP privada principal de la VM
                        if (nicInfo.IsPrimary || string.IsNullOrEmpty(vmInfo.PrivateIpAddress))
                        {
                            vmInfo.PrivateIpAddress = nicInfo.PrivateIpAddress;
                        }

                        // Obtener IP pública si existe
                        if (ipConfig.PublicIPAddress != null)
                        {
                            try
                            {
                                var pipResource = _armClient.GetPublicIPAddressResource(ipConfig.PublicIPAddress.Id);
                                var pip = await pipResource.GetAsync();
                                nicInfo.PublicIpAddress = pip.Value.Data.IPAddress;
                                
                                if (nicInfo.IsPrimary || string.IsNullOrEmpty(vmInfo.PublicIpAddress))
                                {
                                    vmInfo.PublicIpAddress = nicInfo.PublicIpAddress;
                                }
                            }
                            catch
                            {
                                // Error obteniendo IP pública, continuar
                            }
                        }
                    }

                    // Obtener NSGs asociados
                    if (nic.Value.Data.NetworkSecurityGroup != null)
                    {
                        nicInfo.SecurityGroupIds.Add(nic.Value.Data.NetworkSecurityGroup.Id.ToString());
                        
                        var nsgName = nic.Value.Data.NetworkSecurityGroup.Id.Name;
                        if (!vmInfo.NetworkSecurityGroups.Contains(nsgName))
                        {
                            vmInfo.NetworkSecurityGroups.Add(nsgName);
                        }
                    }

                    vmInfo.NetworkInterfaces.Add(nicInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error obteniendo detalles de NIC {nicRef.Id} para VM {vm.Data.Name}");
                }
            }
        }

        return vmInfo;
    }

    private async Task<VirtualNetworkInfo> ConvertToVNetInfoAsync(VirtualNetworkResource vnet)
    {
        var vnetData = vnet.Data;
        
        var vnetInfo = new VirtualNetworkInfo
        {
            Id = vnet.Id.ToString(),
            Name = vnetData.Name,
            ResourceGroup = vnet.Id.ResourceGroupName ?? string.Empty,
            Location = vnetData.Location.ToString(),
            AddressSpaces = vnetData.AddressSpace?.AddressPrefixes?.ToList() ?? new List<string>(),
            DdosProtectionEnabled = vnetData.EnableDdosProtection ?? false,
            Tags = vnetData.Tags?.ToDictionary(t => t.Key, t => t.Value) ?? new Dictionary<string, string>()
        };

        // Obtener subnets
        await foreach (var subnet in vnet.GetSubnets())
        {
            var subnetInfo = ConvertToSubnetInfo(subnet);
            vnetInfo.Subnets.Add(subnetInfo);
        }

        // Obtener peerings
        await foreach (var peering in vnet.GetVirtualNetworkPeerings())
        {
            var peeringInfo = new VirtualNetworkPeering
            {
                Id = peering.Id.ToString(),
                Name = peering.Data.Name,
                RemoteVirtualNetworkId = peering.Data.RemoteVirtualNetwork?.Id?.ToString() ?? string.Empty,
                PeeringState = peering.Data.PeeringState?.ToString() ?? "Unknown",
                AllowVirtualNetworkAccess = peering.Data.AllowVirtualNetworkAccess ?? false,
                AllowForwardedTraffic = peering.Data.AllowForwardedTraffic ?? false,
                AllowGatewayTransit = peering.Data.AllowGatewayTransit ?? false,
                UseRemoteGateways = peering.Data.UseRemoteGateways ?? false
            };
            
            vnetInfo.Peerings.Add(peeringInfo);
        }

        return vnetInfo;
    }

    private SubnetInfo ConvertToSubnetInfo(SubnetResource subnet)
    {
        var subnetData = subnet.Data;
        
        return new SubnetInfo
        {
            Id = subnet.Id.ToString(),
            Name = subnetData.Name,
            AddressPrefix = subnetData.AddressPrefix ?? string.Empty,
            VirtualNetworkName = subnet.Id.Parent?.Name ?? string.Empty,
            NetworkSecurityGroupId = subnetData.NetworkSecurityGroup?.Id?.ToString(),
            RouteTableId = subnetData.RouteTable?.Id?.ToString()
        };
    }
} 