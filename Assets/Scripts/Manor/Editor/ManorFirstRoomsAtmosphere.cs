using System;
using System.Linq;
using Echo.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    public static class ManorFirstRoomsAtmosphere
    {
        public const string Folder="Assets/Props/FirstRoomsAtmosphere";
        public const string Marker="First rooms moonlight and dust";

        [MenuItem("Echo/Освітлення кімнати доглядача та галереї")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }

        public static void Apply()
        {
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var settings=AssetDatabase.LoadAssetAtPath<ManorAtmosphereSettings>(Folder+"/Atmosphere.asset");
            if(settings==null){settings=ScriptableObject.CreateInstance<ManorAtmosphereSettings>();AssetDatabase.CreateAsset(settings,Folder+"/Atmosphere.asset");}
            var owner=GameObject.Find(Marker);var root=owner!=null?owner.transform:Group(Marker);
            // A common subdued base; detailed window lighting below is confined to the first two rooms.
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=settings.ambient;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=settings.haze;RenderSettings.fogDensity=settings.hazeDensity;
            var moon=GameObject.Find("Moonlight - no mains power").GetComponent<Light>();
            moon.intensity=settings.moonIntensity;moon.color=settings.moon;moon.shadows=LightShadows.Soft;
            moon.shadowBias=.035f;moon.shadowNormalBias=.12f;
            // This old broad fill previously lit the gallery through the wall; keep it inside the hall.
            var hallFill=GameObject.Find("Hall west moon bounce");if(hallFill!=null)hallFill.GetComponent<Light>().range=12;

            var caretaker=Spot(root,"Caretaker window moon",new Vector3(-19.65f,2.8f,-8.2f),new Vector3(-15.5f,.3f,-7.8f),settings.caretakerWindowIntensity,10,74,settings.moon);
            caretaker.cookie=WindowCookie();
            for(int i=0;i<3;i++)
            {
                float x=-6.8f+i*4;
                Spot(root,"Gallery moon through window "+(i+1),new Vector3(x-.6f,3.3f,-13.9f),new Vector3(x+.6f,.15f,-7.6f),settings.galleryWindowIntensity,13,43,settings.moon);
            }
            // Gentle reflected light keeps the small key readable inside its shelf.
            Spot(root,"Caretaker reflected moon at cupboard",new Vector3(-11.5f,2.6f,-6.6f),new Vector3(-12,1.15f,-5),1.4f,4.5f,100,settings.moon);
            // Preserve the usability of this existing puzzle after lowering the common ambient base.
            Spot(root,"Library readability bounce",new Vector3(10,2.6f,19),new Vector3(9.25f,.88f,21.2f),4,5,75,settings.moon);
            Lantern(root,settings);
            Dust(root,"Caretaker dust in moonlight",new Vector3(-17.8f,1.7f,-8.0f),new Vector3(2.8f,2.0f,2.4f),settings.dustPerSecond);
            Dust(root,"Gallery dust in moonlight",new Vector3(-2.8f,1.8f,-10.0f),new Vector3(10.0f,2.2f,2.5f),settings.dustPerSecond);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        internal static Light Spot(Transform root,string name,Vector3 position,Vector3 target,float intensity,float range,float angle,Color color)
        {
            var child=root.Find(name);var light=child!=null?child.GetComponent<Light>():new GameObject(name,typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(root,true);light.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            light.type=LightType.Spot;light.color=color;light.intensity=intensity;light.range=range;
            light.spotAngle=angle;light.innerSpotAngle=angle*.72f;light.shadows=LightShadows.Soft;
            light.shadowBias=.025f;light.shadowNormalBias=.08f;light.shadowNearPlane=.1f;
            light.shadowResolution=LightShadowResolution.Medium;light.lightmapBakeType=LightmapBakeType.Realtime;
            return light;
        }

        internal static Texture2D WindowCookie()
        {
            const string path=Folder+"/Four pane moon cookie.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture!=null)return texture;
            texture=new Texture2D(64,64,TextureFormat.RGBA32,false,true){name="Four pane moon cookie",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                bool pane=x>4&&x<59&&y>4&&y<59&&Mathf.Abs(x-31.5f)>2&&Mathf.Abs(y-31.5f)>2;
                texture.SetPixel(x,y,pane?Color.white:Color.black);
            }
            texture.Apply();AssetDatabase.CreateAsset(texture,path);return texture;
        }

        private static void Lantern(Transform root,ManorAtmosphereSettings settings)
        {
            const string name="Oil lantern on caretaker desk";var child=root.Find(name);
            var go=child!=null?child.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorLights/PF_ManorLantern.prefab"));
            go.name=name;go.transform.SetParent(root,true);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,22,0));
            var bounds=BoundsOf(go);var target=new Vector3(-16.25f,.877f,-9.55f);
            go.transform.position=target-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var light=go.GetComponentInChildren<Light>();light.intensity=settings.lanternIntensity;light.range=3.3f;light.color=settings.candleColor;
            var flicker=go.GetComponentInChildren<WarmPropLight>();flicker.flickerAmount=.025f;flicker.flickerSpeed=1.2f;
            foreach(var component in new Object[]{light,flicker}){PrefabUtility.RecordPrefabInstancePropertyModifications(component);EditorUtility.SetDirty(component);}
        }

        private static Material DustMaterial()
        {
            const string path=Folder+"/Faint airborne dust.mat";
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Soft dust mask.asset");
            if(mask==null)
            {
                mask=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft dust mask",wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;mask.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2)));}
                mask.Apply();AssetDatabase.CreateAsset(mask,Folder+"/Soft dust mask.asset");
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Lit")){name="Faint airborne dust"};AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",mask);material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_BlendModePreserveSpecular",0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0);material.SetFloat("_Cull",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
            EditorUtility.SetDirty(material);return material;
        }

        internal static void Dust(Transform root,string name,Vector3 position,Vector3 size,float rate)
        {
            var child=root.Find(name);var particles=child!=null?child.GetComponent<ParticleSystem>():new GameObject(name,typeof(ParticleSystem)).GetComponent<ParticleSystem>();
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);particles.transform.SetParent(root,true);particles.transform.position=position;
            var main=particles.main;main.loop=true;main.playOnAwake=true;main.prewarm=true;main.duration=25;main.startLifetime=25;
            main.startSpeed=new ParticleSystem.MinMaxCurve(.005f,.018f);main.startSize=new ParticleSystem.MinMaxCurve(.005f,.012f);
            main.startColor=new Color(.8f,.84f,.9f,.07f);main.maxParticles=120;main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=false;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=size;
            var emission=particles.emission;emission.rateOverTime=rate;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=DustMaterial();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;renderer.maxParticleSize=.015f;
        }
    }
}
