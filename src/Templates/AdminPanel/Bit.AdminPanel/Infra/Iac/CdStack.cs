using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Compute;
using Pulumi.AzureNative.Network;
using Pulumi.AzureNative.Network.Inputs;
using Pulumi.AzureNative.Resources;
using NetworkSubnetArgs = Pulumi.AzureNative.Network.Inputs.SubnetArgs;

namespace AdminPanel.Iac;

public class CdStack
{
    public CdStack()
    {
        Config pulumiConfig = new();

        ResourceGroup resourceGroup = new("ad-cd", new ResourceGroupArgs
        {
            ResourceGroupName = "ad-cd"
        }, options: new() { ImportId = $"/subscriptions/{GetClientConfig.InvokeAsync().GetAwaiter().GetResult().SubscriptionId}/resourceGroups/ad-cd" });

        var azureDevOpsVMAdminUserName = pulumiConfig.Require($"dev-ops-vm-ad-admin-user-name");
        var azureDevOpsVMAdminUserPassword = pulumiConfig.RequireSecret($"dev-ops-vm-ad-admin-user-password");

        NetworkSecurityGroup azureDevOpsAgentVMNetSecurityGroup = new($"dev-ops-net-sec-group-ad", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            NetworkSecurityGroupName = $"dev-ops-net-sec-group-ad",
            SecurityRules = new List<Pulumi.AzureNative.Network.Inputs.SecurityRuleArgs>
            {
                /*new Pulumi.AzureNative.Network.Inputs.SecurityRuleArgs
                {
                    Name = "RDP",
                    Protocol = SecurityRuleProtocol.Tcp,
                    SourcePortRange = "*",
                    DestinationPortRange = "3389",
                    SourceAddressPrefix ="*",
                    DestinationAddressPrefix = "*",
                    Access= SecurityRuleAccess.Allow,
                    Priority = 300,
                    Direction= SecurityRuleDirection.Inbound
                }*/
            }
        });

        VirtualNetwork network = new($"dev-ops-net-ad", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            VirtualNetworkName = $"dev-ops-net-ad",
            AddressSpace = new AddressSpaceArgs { AddressPrefixes = new List<string> { "10.0.0.0/16" } }
        });

        PublicIPAddress ipAddress = new($"dev-ops-ip-ad", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            PublicIpAddressName = $"dev-ops-ip-ad",
            PublicIPAllocationMethod = IPAllocationMethod.Static,
            Sku = new PublicIPAddressSkuArgs { Name = Pulumi.AzureNative.Network.PublicIPAddressSkuName.Standard },
            DnsSettings = new PublicIPAddressDnsSettingsArgs
            {
                DomainNameLabel = $"dev-ops-vm-ad" // dev-ops-vm-ad.eastus.cloudapp.azure.com
            }
        });

        Subnet subnet = new Subnet($"dev-ops-subnet-ad", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Name = $"dev-ops-subnet-ad",
            SubnetName = $"dev-ops-subnet-ad",
            VirtualNetworkName = network.Name,
            AddressPrefix = "10.0.2.0/24"
        });

        NetworkInterface networkInterface = new($"dev-ops-net-interface", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            NetworkInterfaceName = $"dev-ops-net-interface",
            // EnableAcceleratedNetworking = true, // Depends on your vm size
            DnsSettings = new NetworkInterfaceDnsSettingsArgs
            {
                InternalDnsNameLabel = $"dev-ops-vm-ad" // dev-ops-vm-ad.internal.cloudapp.net
            },
            IpConfigurations = new List<NetworkInterfaceIPConfigurationArgs>
            {
                new NetworkInterfaceIPConfigurationArgs
                {
                    Name = $"dev-ops-vm-ip-ad",
                    PrivateIPAddress = "10.0.0.4",
                    PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                    PublicIPAddress = new Pulumi.AzureNative.Network.Inputs.PublicIPAddressArgs
                    {
                        Id = ipAddress.Id,
                        IpAddress = ipAddress.IpAddress
                    },
                    Subnet = new NetworkSubnetArgs { Id = subnet.Id } ,
                    Primary = true
                }
            },
            NetworkSecurityGroup = new Pulumi.AzureNative.Network.Inputs.NetworkSecurityGroupArgs
            {
                Location = resourceGroup.Location,
                Id = azureDevOpsAgentVMNetSecurityGroup.Id
            }
        });

        VirtualMachine vm = new($"dev-ops-vm-ad", new()
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            VmName = $"dev-ops-vm-ad",
            OsProfile = new Pulumi.AzureNative.Compute.Inputs.OSProfileArgs
            {
                AdminUsername = azureDevOpsVMAdminUserName,
                AdminPassword = azureDevOpsVMAdminUserPassword,
                ComputerName = $"dev-ops-vm-ad",
                WindowsConfiguration = new Pulumi.AzureNative.Compute.Inputs.WindowsConfigurationArgs { ProvisionVMAgent = true, EnableAutomaticUpdates = true }
            },
            HardwareProfile = new Pulumi.AzureNative.Compute.Inputs.HardwareProfileArgs
            {
                VmSize = VirtualMachineSizeTypes.Standard_B1ms
            },
            StorageProfile = new Pulumi.AzureNative.Compute.Inputs.StorageProfileArgs
            {
                ImageReference = new Pulumi.AzureNative.Compute.Inputs.ImageReferenceArgs { Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer", Sku = "2019-Datacenter", Version = "latest" },
                OsDisk = new Pulumi.AzureNative.Compute.Inputs.OSDiskArgs
                {
                    ManagedDisk = new Pulumi.AzureNative.Compute.Inputs.ManagedDiskParametersArgs { StorageAccountType = StorageAccountTypes.Standard_LRS },
                    OsType = OperatingSystemTypes.Windows,
                    Name = $"dev-ops-vm-disk-ad",
                    CreateOption = DiskCreateOptionTypes.FromImage,
                    Caching = CachingTypes.ReadWrite,
                    DiskSizeGB = 127
                }
            },
            NetworkProfile = new Pulumi.AzureNative.Compute.Inputs.NetworkProfileArgs
            {
                NetworkInterfaces = new List<Pulumi.AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs>
                {
                    new Pulumi.AzureNative.Compute.Inputs.NetworkInterfaceReferenceArgs { Id = networkInterface.Id }
                }
            }
        });
    }
}
