using System;
using System.Linq;
using Echo.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    // Scene authoring only: no extra gameplay manager or input component.
    public static class ManorWholeAtmosphere
    {
        public const string Marker="Manor moonlight and candle accents";
        [MenuItem("Echo/Освітлення всього маєтку")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype first.");
            ManorFirstRoomsAtmosphere.Apply();Apply();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            var settings=AssetDatabase.LoadAssetAtPath<ManorAtmosphereSettings>(ManorFirstRoomsAtmosphere.Folder+"/Atmosphere.asset");
            var existing=GameObject.Find(Marker);var root=existing!=null?existing.transform:new GameObject(Marker).transform;
            // Retire broad shadowless fills that washed out neighbouring rooms through walls.
            foreach(var light in Object.FindObjectsByType<Light>())
                if(light.name.EndsWith("moon bounce"))light.enabled=false;

            Window(root,"Hall south window",new Vector3(23.55f,3.1f,-8.5f),new Vector3(15,.15f,-5),32*settings.hallLightScale,16,48,settings);
            Window(root,"Hall north window",new Vector3(23.55f,3.1f,3.8f),new Vector3(15,.2f,1),30*settings.hallLightScale,16,52,settings);
            Beam(root,"Hall stair reflected light",new Vector3(6,5.2f,2),new Vector3(6,.4f,5),5*settings.hallLightScale,8,90,settings);
            Window(root,"Library east window",new Vector3(23.55f,2.5f,16),new Vector3(16,.1f,14),26*settings.libraryLightScale,13,58,settings);
            Beam(root,"Library cache reflected light",new Vector3(7,3,19),new Vector3(6,1,20.5f),3*settings.libraryLightScale,5,82,settings);
            Window(root,"Study west window",new Vector3(-9.55f,6.1f,-5.5f),new Vector3(-4.5f,4.45f,-3),20*settings.upperFloorLightScale,10,65,settings);
            Beam(root,"Upper landing reflected light",new Vector3(3,7.4f,5.7f),new Vector3(3,4.5f,5.5f),4*settings.upperFloorLightScale,6,100,settings);
            Beam(root,"Study painting reflected light",new Vector3(-5,7.4f,1),new Vector3(-5,5.6f,3.9f),4*settings.upperFloorLightScale,6,85,settings);
            Beam(root,"Kitchen reflected night light",new Vector3(34.8f,3.4f,5),new Vector3(30,.3f,6),11*settings.kitchenLightScale,9,92,settings);
            Beam(root,"Kitchen winch reflected light",new Vector3(34.5f,3.2f,11),new Vector3(32.8f,.9f,12),5*settings.kitchenLightScale,5,78,settings);
            for(int i=0;i<3;i++)Window(root,"Conservatory window "+i,new Vector3(37.55f,3.1f,18.5f+3.5f*i),new Vector3(30,.15f,17.3f+3.5f*i),23*settings.gardenLightScale,12,53,settings);
            Beam(root,"Conservatory latch reflected light",new Vector3(31.5f,3.8f,25.8f),new Vector3(30,1.2f,27.7f),3*settings.gardenLightScale,5,72,settings);
            // The open yard already receives the common directional moon. Small pools reveal plants and the shed.
            Beam(root,"Yard medicinal bed moon",new Vector3(20.5f,5,34.5f),new Vector3(19.2f,.5f,33.2f),8*settings.yardLightScale,8,68,settings);
            Beam(root,"Yard garden path moon",new Vector3(28,5,32),new Vector3(28,.2f,36),9*settings.yardLightScale,12,76,settings);
            Lantern(root,"Study desk candle lantern","Study writing desk",new Vector3(-6.1f,0,-6.4f),settings.lanternIntensity*.85f*settings.upperFloorLightScale);
            Lantern(root,"Kitchen preparation candle lantern","Kitchen Preparation Island",new Vector3(32.85f,0,6.5f),settings.lanternIntensity*settings.kitchenLightScale);
            Lantern(root,"Potting shed candle lantern","Shed Tool Table",new Vector3(33.65f,0,38.1f),settings.lanternIntensity*settings.yardLightScale);
            Dust(root,"Hall dust",new Vector3(19,2,-4),new Vector3(5,3,5),settings);
            Dust(root,"Library dust",new Vector3(20,2,16),new Vector3(4,2.8f,4),settings);
            Dust(root,"Study dust",new Vector3(-6.5f,6,-4.5f),new Vector3(4,2,4),settings);
            Dust(root,"Conservatory dust",new Vector3(34,2.2f,22),new Vector3(4,2.8f,8),settings);
            Dust(root,"Shed dust",new Vector3(32.5f,1.7f,38),new Vector3(3,2,3),settings);
            // URP uses its own resolution tier rather than Light.shadowResolution.
            // Keep the existing 2048 atlas; small local sources need only its low tier.
            foreach(var light in root.GetComponentsInChildren<Light>().Concat(GameObject.Find(ManorFirstRoomsAtmosphere.Marker).GetComponentsInChildren<Light>()))
            {
                var data=light.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                if(data==null)data=light.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                var serialized=new SerializedObject(data);serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();
                if(PrefabUtility.IsPartOfPrefabInstance(data))PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            }
            SoftGlow(root,settings);
            EditorUtility.SetDirty(settings);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static void SoftGlow(Transform root,ManorAtmosphereSettings settings)
        {
            const string path=ManorFirstRoomsAtmosphere.Folder+"/Twilight glow.asset";
            var profile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
            if(profile==null){profile=ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
            if(!profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
            {bloom=profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.active=true;bloom.intensity.Override(settings.candleBloom);bloom.threshold.Override(1.05f);bloom.scatter.Override(.65f);
            bloom.tint.Override(new Color(1,.94f,.84f));bloom.highQualityFiltering.Override(false);
            EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(profile);
            var child=root.Find("Soft twilight glow");var volume=child!=null?child.GetComponent<UnityEngine.Rendering.Volume>():new GameObject("Soft twilight glow",typeof(UnityEngine.Rendering.Volume)).GetComponent<UnityEngine.Rendering.Volume>();
            volume.transform.SetParent(root,false);volume.isGlobal=true;volume.priority=5;volume.sharedProfile=profile;
            foreach(var camera in Object.FindObjectsByType<Camera>())
            {
                camera.allowHDR=true;
                var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if(data==null)data=camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing=true;
                if(PrefabUtility.IsPartOfPrefabInstance(camera))PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                if(PrefabUtility.IsPartOfPrefabInstance(data))PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            }
        }
        static Light Beam(Transform root,string name,Vector3 p,Vector3 target,float intensity,float range,float angle,ManorAtmosphereSettings s)
            =>ManorFirstRoomsAtmosphere.Spot(root,name,p,target,intensity,range,angle,s.moon);
        static void Window(Transform root,string name,Vector3 p,Vector3 target,float intensity,float range,float angle,ManorAtmosphereSettings s)
        {Beam(root,name,p,target,intensity,range,angle,s).cookie=ManorFirstRoomsAtmosphere.WindowCookie();}
        static void Dust(Transform root,string name,Vector3 p,Vector3 size,ManorAtmosphereSettings s)
        {ManorFirstRoomsAtmosphere.Dust(root,name,p,size,Mathf.Min(2,s.dustPerSecond));root.Find(name).GetComponent<ParticleSystem>().maxParticles=70;}
        static void Lantern(Transform root,string name,string supportName,Vector3 p,float intensity)
        {
            var support=GameObject.Find(supportName);
            if(support==null)throw new InvalidOperationException("Lantern support missing: "+supportName);
            var surfaces=support.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            float top=surfaces.Length>0?surfaces.Max(r=>r.bounds.max.y):support.GetComponentsInChildren<Collider>().Max(c=>c.bounds.max.y);
            var child=root.Find(name);var go=child!=null?child.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorLights/PF_ManorLantern.prefab"));
            go.name=name;go.transform.SetParent(root,true);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,15,0));
            var bounds=go.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            go.transform.position=new Vector3(p.x,top+.004f,p.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            // Dressing stays clear of interaction rays and cannot add an invisible movement obstacle.
            foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=false;
            var light=go.GetComponentInChildren<Light>();light.intensity=intensity;light.range=3.6f;light.color=AssetDatabase.LoadAssetAtPath<ManorAtmosphereSettings>(ManorFirstRoomsAtmosphere.Folder+"/Atmosphere.asset").candleColor;light.shadows=LightShadows.Soft;
            var flicker=go.GetComponentInChildren<WarmPropLight>();flicker.flickerAmount=.025f;flicker.flickerSpeed=1.2f;
            foreach(var component in go.GetComponentsInChildren<Component>().Where(c=>c is Light||c is Collider||c is WarmPropLight))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
}
