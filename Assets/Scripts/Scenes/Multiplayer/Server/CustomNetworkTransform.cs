using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CustomNetworkTransform : NetworkTransform
{

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}