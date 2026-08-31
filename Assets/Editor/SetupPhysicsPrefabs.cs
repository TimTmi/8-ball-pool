using UnityEngine;
using UnityEditor;
using EightBall.Gameplay;

namespace EightBall.EditorScripts
{
    public class SetupPhysicsPrefabs
    {
        [MenuItem("Tools/Setup Physics Prefabs")]
        public static void SetupPrefabs()
        {
            // 1. Load Physics Materials
            PhysicsMaterial2D ballMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/PhysicsMaterials/BallMaterial.physicsMaterial2D");
            PhysicsMaterial2D cushionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/PhysicsMaterials/CushionMaterial.physicsMaterial2D");

            if (ballMat == null || cushionMat == null)
            {
                Debug.LogError("Physics materials not found at expected paths.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 2. Create Ball Prefab
            GameObject ballObj = new GameObject("Ball");
            SpriteRenderer sr = ballObj.AddComponent<SpriteRenderer>();
            // Use a default knob sprite as placeholder
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            
            CircleCollider2D circleCollider = ballObj.AddComponent<CircleCollider2D>();
            circleCollider.sharedMaterial = ballMat;
            circleCollider.radius = TableLayout.BallRadius; // Diameter TableLayout.BallDiameter

            Rigidbody2D rb = ballObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 0.17f; // Approx mass of pool ball in kg
            rb.linearDamping = 0.8f; // Simulates rolling friction on table
            rb.angularDamping = 1.0f; // Simulates spin decay
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevent passing through cushions
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement

            PrefabUtility.SaveAsPrefabAsset(ballObj, "Assets/Prefabs/Ball.prefab");
            Object.DestroyImmediate(ballObj);

            // 3. Create Cushion Prefab
            GameObject cushionObj = new GameObject("Cushion");
            BoxCollider2D boxCollider = cushionObj.AddComponent<BoxCollider2D>();
            boxCollider.sharedMaterial = cushionMat;
            boxCollider.size = new Vector2(10f, 0.5f);

            PrefabUtility.SaveAsPrefabAsset(cushionObj, "Assets/Prefabs/Cushion.prefab");
            Object.DestroyImmediate(cushionObj);
            
            // 4. Create Pocket Prefab (Trigger)
            GameObject pocketObj = new GameObject("Pocket");
            CircleCollider2D pocketCollider = pocketObj.AddComponent<CircleCollider2D>();
            pocketCollider.isTrigger = true;
            pocketCollider.radius = TableLayout.PocketRadius;
            
            PrefabUtility.SaveAsPrefabAsset(pocketObj, "Assets/Prefabs/Pocket.prefab");
            Object.DestroyImmediate(pocketObj);

            Debug.Log("Physics Prefabs (Ball, Cushion, Pocket) created successfully in Assets/Prefabs.");
        }
    }
}
