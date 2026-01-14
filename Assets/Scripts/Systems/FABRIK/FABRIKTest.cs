using Arielado.FABRIK;
using UnityEngine;

[ExecuteAlways]
public class FABRIKTest : MonoBehaviour {
    public Transform root, tip, target;

    public Vector3 origin;

    public IKChain chain;

    private void Update() {
        if (root == null || tip == null || target == null) return;

        if (chain.joints == null || chain.joints.Length == 0) {
            chain = FABRIK.CreateChain(root, tip);
        }

        IKChain.Solve(ref chain, target);
    }
}
