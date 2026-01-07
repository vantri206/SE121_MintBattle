using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using System.Numerics;
using UnityEngine;
public class SignatureService : MonoBehaviour
{
    private readonly string adminPrivateKey = "b20cbc1cffef1d73664b5464dc11298e7b12cedd67b5bd95083ad5c2841b5abc";
    //only for demo test, need deploy server and private storage
    void Start()
    {
        var signer = new EthereumMessageSigner();
        var key = new Nethereum.Signer.EthECKey(adminPrivateKey);
        string myPublicAddress = key.GetPublicAddress();

        Debug.Log($"[CHECK] C# Private Key address: {myPublicAddress}");
    }
    public string GenerateWeaponSignature(string ownerItemAddress, BigInteger tokenId, int amount, long uid)
    {
        var abiEncode = new ABIEncode();
        string addressClean = ownerItemAddress.Trim().ToLower();
        if (!addressClean.StartsWith("0x")) addressClean = "0x" + addressClean;
        Debug.Log($"[Server Sign] Address: {addressClean} | ID: {tokenId} | Amt: {amount} | UID: {uid}");

        byte[] messageHash = abiEncode.GetSha3ABIEncodedPacked(
            new ABIValue("address", addressClean),
            new ABIValue("uint256", tokenId),
            new ABIValue("uint256", new BigInteger(amount)),
            new ABIValue("uint256", new BigInteger(uid))
        );

        var signer = new EthereumMessageSigner();
        return signer.Sign(messageHash, adminPrivateKey);
    }
}