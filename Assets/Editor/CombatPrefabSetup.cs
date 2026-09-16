using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit setup helper; never replaces existing prefabs.
public static class CombatPrefabSetup
{
    private const string Folder = "Assets/Prefabs";
    [MenuItem("Double Dungeon/Create Missing Combat Prefabs")]
    public static void CreateMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (File.Exists(Folder + "/Player.prefab") && File.Exists(Folder + "/Gun.prefab")
            && File.Exists(Folder + "/Bullet.prefab")) return;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        string texturePath = Folder + "/PrototypeSquare.png";
        if (!File.Exists(texturePath))
        {
            var texture = new Texture2D(8, 8);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 8;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        if (sprite == null) throw new System.InvalidOperationException("Prototype square sprite could not be imported.");
        string bulletPath = Folder + "/Bullet.prefab";
        if (!File.Exists(bulletPath))
        {
            var bullet = new GameObject("Bullet");
            try
            {
                Visual(bullet, sprite, new Color(1, 0.8f, 0.15f), 3);
                bullet.transform.localScale = new Vector3(0.25f, 0.12f, 1);
                var body = bullet.AddComponent<Rigidbody2D>();
                body.gravityScale = 0;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                bullet.AddComponent<BoxCollider2D>().isTrigger = true;
                bullet.AddComponent<Bullet>();
                PrefabUtility.SaveAsPrefabAsset(bullet, bulletPath);
            }
            finally { Object.DestroyImmediate(bullet); }
        }
        string gunPath = Folder + "/Gun.prefab";
        if (!File.Exists(gunPath))
        {
            var gun = new GameObject("Gun");
            try
            {
                var behavior = gun.AddComponent<CardinalGun>();
                behavior.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath).GetComponent<Bullet>();
                var barrel = new GameObject("Barrel");
                barrel.transform.SetParent(gun.transform, false);
                barrel.transform.localPosition = new Vector3(0.6f, 0, 0);
                barrel.transform.localScale = new Vector3(0.65f, 0.2f, 1);
                Visual(barrel, sprite, new Color(0.25f, 0.3f, 0.4f), 2);
                var muzzle = new GameObject("Muzzle");
                muzzle.transform.SetParent(gun.transform, false);
                muzzle.transform.localPosition = new Vector3(1.08f, 0, 0);
                behavior.muzzle = muzzle.transform;
                PrefabUtility.SaveAsPrefabAsset(gun, gunPath);
            }
            finally { Object.DestroyImmediate(gun); }
        }
        string playerPath = Folder + "/Player.prefab";
        if (!File.Exists(playerPath))
        {
            var player = new GameObject("Player");
            try
            {
                Visual(player, sprite, new Color(0.2f, 0.9f, 0.65f), 1);
                var movement = player.AddComponent<ArduinoJoystickPlayer>();
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(gunPath), player.transform);
                movement.gun = gun.GetComponent<CardinalGun>();
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally { Object.DestroyImmediate(player); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Player, Gun and Bullet prefabs created in Assets/Prefabs. Drag Player into your test scene and set its COM port.");
    }

    private static void Visual(GameObject obj, Sprite sprite, Color color, int order)
    {
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
    }
}
