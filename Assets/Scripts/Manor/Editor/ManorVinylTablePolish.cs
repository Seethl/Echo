using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    // Retain puzzle objects and references; replace only the former upright presentation.
    public static class ManorVinylTablePolish
    {
        public const string TableName="Library vinyl table";
        private static readonly Vector3 Centre=new Vector3(9.25f,0,21.31f);
        [MenuItem("Echo/Перетворити стенд платівок на стіл")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();
            if(puzzle==null)throw new InvalidOperationException("Library puzzle is missing.");
            var table=GameObject.Find(TableName);
            if(table==null)
            {
                var root=Group(TableName);root.position=Centre;
                var wood=AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/Aged timber.mat");
                var brass=AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/Dull brass.mat");
                Part("Vinyl table top",root,new Vector3(0,.83f,0),new Vector3(2.9f,.10f,1.02f),wood);
                foreach(float x in new[]{-1.27f,1.27f})foreach(float z in new[]{-.34f,.34f})
                {
                    Part("Vinyl table leg",root,new Vector3(x,.4f,z),new Vector3(.11f,.8f,.11f),wood);
                    Part("Vinyl table foot cap",root,new Vector3(x,.055f,z),new Vector3(.115f,.11f,.115f),brass,false);
                }
                foreach(float z in new[]{-.39f,.39f})Part("Vinyl table apron",root,new Vector3(0,.715f,z),new Vector3(2.65f,.16f,.055f),wood);
                foreach(float x in new[]{-1.27f,1.27f})Part("Vinyl table side apron",root,new Vector3(x,.715f,0),new Vector3(.055f,.16f,.8f),wood);
                // The old input plaque is inclined on these two small brackets, not suspended in space.
                foreach(float x in new[]{-.9f,.9f})Part("Vinyl display support",root,new Vector3(x,.935f,.33f),new Vector3(.045f,.11f,.12f),wood,false);
                table=root.gameObject;
            }
            // Also correct the pose when an earlier editor migration is re-applied.
            table.transform.SetPositionAndRotation(Centre,Quaternion.identity);table.transform.localScale=Vector3.one;
            var obstacle=table.GetComponent<NavMeshObstacle>();if(obstacle==null)obstacle=table.AddComponent<NavMeshObstacle>();
            obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0,.44f,0);obstacle.size=new Vector3(2.94f,.9f,1.06f);obstacle.carving=true;
            var old=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t=>t.name=="Cache Input Board");
            if(old!=null)old.gameObject.SetActive(false);
            var buttons=Object.FindObjectsByType<SymbolButton>().Where(b=>b.puzzle==puzzle&&!b.demonstration).ToArray();
            if(buttons.Length!=3)throw new InvalidOperationException("Expected three library records.");
            foreach(var button in buttons)
            {
                int slot=button.symbolId=="bird"?0:button.symbolId=="wolf"?1:2;
                button.transform.SetParent(table.transform,true);
                button.transform.SetPositionAndRotation(new Vector3(8.2f+slot*.7f,.894f,21.18f),Quaternion.Euler(90,0,0));
                button.transform.localScale=Vector3.one*.8f;
            }
            var display=puzzle.inputDisplay;display.transform.SetParent(table.transform,true);
            display.transform.SetPositionAndRotation(new Vector3(9.15f,.992f,21.63f),Quaternion.Euler(65,0,0));
            display.transform.localScale=Vector3.one;display.characterSize=.012f;
            var plaque=GameObject.Find("Compact library input plaque");plaque.transform.SetParent(table.transform,true);
            plaque.transform.SetPositionAndRotation(display.transform.position+display.transform.forward*.025f,display.transform.rotation);
            plaque.transform.localScale=new Vector3(2.45f,.19f,.045f);
            var reset=Object.FindAnyObjectByType<SymbolReset>();reset.transform.SetParent(table.transform,true);
            reset.transform.SetPositionAndRotation(new Vector3(10.3f,.915f,21.18f),Quaternion.Euler(90,0,0));
            reset.transform.localScale=new Vector3(.40f,.24f,.06f);
            var label=GameObject.Find("Reset Label").GetComponent<TextMesh>();label.transform.SetParent(table.transform,true);
            label.transform.SetPositionAndRotation(new Vector3(10.3f,.948f,21.18f),Quaternion.Euler(90,0,0));
            label.transform.localScale=Vector3.one;label.characterSize=.010f;label.text="Скинути";
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        private static void Part(string name,Transform parent,Vector3 local,Vector3 size,Material material,bool solid=true)
        {var go=Box(name,null,Vector3.zero,size,material,solid);go.transform.SetParent(parent,false);go.transform.localPosition=local;}
    }
}
