using System;
using System.Collections.Generic;
using System.Linq;
using Adventure.Obstacles;
using Adventure.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KitchenPhysicsPropSetup
{
    const string MeshFolder = "Assets/_Project/Meshes/KitchenPhysics";
    const string PrefabFolder = "Assets/_prefab/厨房/物理道具";
    static readonly HashSet<string> Names = new HashSet<string> {
        "Potato", "Bowl", "Plate", "Lemon", "Cup", "Carrot"
    };

    [MenuItem("Adventure/Kitchen/Set Up Physical Props")]
    public static void Install() { Debug.Log(InstallScene()); }

    public static string InstallScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.name != "Kitchen_Adventure")
            throw new InvalidOperationException("Open Kitchen_Adventure in Edit mode first.");
        Folder(MeshFolder); Folder(PrefabFolder);
        var targets = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(m => m.gameObject.scene == scene && m.sharedMesh != null && Names.Contains(m.sharedMesh.name)).ToArray();
        var counts = new SortedDictionary<string, int>();
        foreach (var mf in targets)
        {
            string name = mf.sharedMesh.name;
            // Idempotent: user tuning on an already converted body is retained.
            if (mf.GetComponent<PushableProp>() == null) Configure(mf);
            if (!counts.ContainsKey(name)) counts[name] = 0;
            counts[name]++;
        }
        // Liquids are visual contents of a vessel, not separate floating static scenery.
        int contents = 0;
        foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.gameObject.scene != scene || mf.sharedMesh == null) continue;
            string vessel = mf.sharedMesh.name == "Cup_Coffee" ? "Cup" : null;
            if (vessel == null || mf.GetComponentInParent<PushableProp>() != null) continue;
            var nearest = targets.Where(t => t.sharedMesh.name == vessel && t.transform.parent == mf.transform.parent)
                .OrderBy(t => Vector3.SqrMagnitude(t.transform.position - mf.transform.position)).FirstOrDefault();
            if (nearest == null) continue;
            var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(mf.gameObject);
            if (outer != null) PrefabUtility.UnpackPrefabInstance(outer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Undo.SetTransformParent(mf.transform, nearest.transform, "Attach vessel contents");
            GameObjectUtility.SetStaticEditorFlags(mf.gameObject, 0);
            foreach (var c in mf.GetComponents<Collider>()) Undo.DestroyObjectImmediate(c);
            contents++;
        }
        foreach (string name in counts.Keys)
        {
            string path = PrefabFolder + "/PF_" + name + "_Physics.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
            var sample = targets.First(t => t.sharedMesh.name == name);
            var clone = UnityEngine.Object.Instantiate(sample.gameObject);
            clone.name = "PF_" + name + "_Physics";
            clone.transform.SetParent(null);
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(clone, path);
            UnityEngine.Object.DestroyImmediate(clone);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Physical props: " + targets.Length + "; prefab types: " + counts.Count + "; attached contents: " + contents + "\n" +
            string.Join("\n", counts.Select(p => p.Key + ": " + p.Value));
    }

    static void Configure(MeshFilter mf)
    {
        GameObject go = mf.gameObject;
        string name = mf.sharedMesh.name;
        foreach (var c in go.GetComponents<Collider>()) Undo.DestroyObjectImmediate(c);
        GameObjectUtility.SetStaticEditorFlags(go, 0);
        var body = Undo.AddComponent<Rigidbody>(go);
        body.mass = Mass(name);
        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearDamping = 0.15f;
        body.angularDamping = 0.25f;
        body.maxAngularVelocity = 12f;
        body.maxDepenetrationVelocity = 1.5f;
        body.solverIterations = 10;
        body.solverVelocityIterations = 4;
        body.sleepThreshold = 0.02f;
        Undo.AddComponent<PushableProp>(go);
        if (go.GetComponent<CameraObstructionIgnore>() == null) Undo.AddComponent<CameraObstructionIgnore>(go);
        if (name == "Bowl" || name == "Cup")
            Container(mf);
        else
            AddHull(go, name, mf.sharedMesh.vertices);
        EditorUtility.SetDirty(go);
    }

    static float Mass(string name)
    {
        switch(name)
        {
            case "Bowl": return 0.65f;
            case "Plate": return 0.6f;
            case "Cup": return 0.35f;
            case "Potato": return 0.22f;
            default: return 0.2f;
        }
    }

    static void Container(MeshFilter mf)
    {
        string name = mf.sharedMesh.name;
        Bounds b = mf.sharedMesh.bounds;
        float low = b.min.y, high = b.max.y;
        float radius = b.extents.x;
        Vector3 center = Vector3.zero;
        float bottomRadius = radius * 0.8f;
        float wall = Mathf.Clamp(radius * 0.07f, 0.012f, 0.04f);
        if (name == "Bowl") bottomRadius = radius * 0.4f;
        if (name == "Cup")
        { radius = b.extents.z; center.x = b.min.x + radius; bottomRadius = radius * 0.8f; }
        var bottom = new List<Vector3>();
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8;
            foreach(float y in new[]{low,low+wall})
                bottom.Add(center + new Vector3(Mathf.Cos(a)*bottomRadius,y,Mathf.Sin(a)*bottomRadius));
        }
        AddHull(Child(mf.gameObject,"Bottom"),name+"_Bottom",bottom.ToArray());
        int bands = name == "Bowl" ? 2 : 1;
        for(int band=0;band<bands;band++)
        for(int segment=0;segment<12;segment++)
        {
            var points = new List<Vector3>();
            for(int level=0;level<2;level++)
            {
                float t = (band+level)/(float)bands;
                float r = Mathf.Lerp(bottomRadius,radius, name=="Bowl" ? Mathf.Sqrt(t) : t);
                float y = Mathf.Lerp(low+wall*0.5f,high,t);
                for(int edge=0;edge<2;edge++)
                {
                    float angle = (segment+edge)*Mathf.PI/6;
                    for(int side=0;side<2;side++)
                    {
                        float rr = r - side*wall;
                        points.Add(center+new Vector3(Mathf.Cos(angle)*rr,y,Mathf.Sin(angle)*rr));
                    }
                }
            }
            AddHull(Child(mf.gameObject,"Wall_"+band+"_"+segment),name+"_Wall_"+band+"_"+segment,points.ToArray());
        }
        if(name=="Cup")
        {
            // Three small boxes retain the opening in the handle.
            float start=center.x+radius*0.85f, end=b.max.x;
            float yTop=high*0.75f, yBottom=low*0.65f;
            Box(Child(mf.gameObject,"HandleTop"),new Vector3((start+end)*.5f,yTop,0),new Vector3(end-start,wall*1.4f,wall*1.6f));
            Box(Child(mf.gameObject,"HandleBottom"),new Vector3((start+end)*.5f,yBottom,0),new Vector3(end-start,wall*1.4f,wall*1.6f));
            Box(Child(mf.gameObject,"HandleOuter"),new Vector3(end-wall*.5f,(yTop+yBottom)*.5f,0),new Vector3(wall,yTop-yBottom,wall*1.6f));
        }
    }

    static GameObject Child(GameObject parent,string name)
    {
        var child = new GameObject("Physics_"+name);
        Undo.RegisterCreatedObjectUndo(child,"Create prop collider");
        child.transform.SetParent(parent.transform,false);
        return child;
    }
    static void Box(GameObject go,Vector3 center,Vector3 size)
    {
        var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;c.contactOffset=0.005f;
    }
    static void AddHull(GameObject go,string name,Vector3[] source)
    {
        string path = MeshFolder+"/"+name+"_Collision.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var points=new List<Vector3>();
            if(source.Length<=96)points.AddRange(source.Distinct());
            else
            {
                // Support points form a bounded, low-poly convex hull (at most 86 vertices).
                for(int latitude=0;latitude<=7;latitude++)
                for(int longitude=0;longitude<12;longitude++)
                {
                    float phi=Mathf.PI*latitude/7,theta=Mathf.PI*longitude/6;
                    var direction=new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta));
                    Vector3 best=source[0];float dot=float.NegativeInfinity;
                    foreach(var v in source){float d=Vector3.Dot(v,direction);if(d>dot){dot=d;best=v;}}
                    if(!points.Contains(best))points.Add(best);
                }
            }
            mesh=new Mesh{name=name+"_Collision"};mesh.SetVertices(points);
            var triangles=new List<int>();for(int i=1;i<points.Count-1;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);}
            mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);
        }
        var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;collider.contactOffset=0.005f;
    }
    static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
    }
}
