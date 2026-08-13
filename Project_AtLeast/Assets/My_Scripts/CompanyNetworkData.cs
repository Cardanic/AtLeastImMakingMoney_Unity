// CompanyNetworkData.cs — attach to each company GameObject
using Unity.Netcode;
using Unity.Collections;

public class CompanyNetworkData : NetworkBehaviour
{
    // NetworkVariable syncs automatically to all clients
    public NetworkVariable<float> stockPrice = 
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, 
                                       NetworkVariableWritePermission.Server);

    public NetworkVariable<float> marketCap = 
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, 
                                       NetworkVariableWritePermission.Server);

    public NetworkVariable<float> priceChange = 
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, 
                                       NetworkVariableWritePermission.Server);

    // FixedString for text — regular strings don't work in NetworkVariable
    public NetworkVariable<FixedString64Bytes> companyName = 
        new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, 
                                                    NetworkVariableWritePermission.Server);

    // Called by your existing simulation logic on the host
    public void UpdateData(string name, float price, float cap, float change)
    {
        if (!IsServer) return; // only host can write

        companyName.Value = new FixedString64Bytes(name);
        stockPrice.Value = price;
        marketCap.Value = cap;
        priceChange.Value = change;
    }
}