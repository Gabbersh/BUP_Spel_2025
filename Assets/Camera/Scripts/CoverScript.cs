using UnityEngine;

public class CoverScript : MonoBehaviour
{
    void LateUpdate()
    {
        transform.SetAsFirstSibling();
    }
}
