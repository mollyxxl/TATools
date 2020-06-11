using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ShaderReferenceInMaterial
{
	public Shader shader = null;
	public int shaderType = 0;
	public int materialCount = 0;
	public bool showMaterial = false;
	public List<Material> usedInMaterial = new List<Material> ();
	public ShaderReferenceInMaterial(){}
}

public class ShaderChecker : EditorWindow 
{
	static Shader[] m_selfShader = null;
	static Material[] m_materials = null;
	
	string[] inspectToolbarStrings = 
	{
		"资源查找：查找未被材质球使用的shader",
		"引用查找：查找当前场景使用到的shader"
	};
	
	enum InspectType 
	{
		Project,
		Hierarchy
	};
	
	private InspectType ActiveInspectType=InspectType.Project;
	private static List<ShaderReferenceInMaterial> shaderDetailList= new List<ShaderReferenceInMaterial>();
	
	
	[MenuItem("XXL/ResourcesCheck/查找材质和Shader")]
	static void Init ()
	{
		ShaderChecker window = (ShaderChecker) EditorWindow.GetWindow (typeof (ShaderChecker));
        window.Show();
        //window.LoadResources ();
	}
	
	private Vector2 m_scrollPoint = Vector2.zero;
	void OnGUI ()
	{
		if (GUILayout.Button ("Check"))
			LoadResources ();
		if (GUILayout.Button ("Refresh"))
			CheckResources ();
		ActiveInspectType=(InspectType)GUILayout.Toolbar((int)ActiveInspectType, inspectToolbarStrings);
		
		switch(ActiveInspectType)
		{
		    case InspectType.Project:
			    ProjectWindow();
			    break;
		    case InspectType.Hierarchy:
			    HierarchyWindow();
			    break;
		    default:
			    break;
		}
		Repaint ();
	}
	
	private void ProjectWindow()
	{
		m_scrollPoint = GUILayout.BeginScrollView (m_scrollPoint);
		
		IEnumerable<ShaderReferenceInMaterial> query = null;
		query = from items in shaderDetailList orderby items.shaderType select items;
		
		foreach (ShaderReferenceInMaterial sri in query)
		{
			if(null == sri.shader)
				continue;
			
			GUILayout.BeginHorizontal ();
			if(GUILayout.Button(sri.shader.name,GUILayout.Width(300)))
			{
				Selection.activeObject = sri.shader;
			}
			GUILayout.Label(sri.materialCount.ToString()+"个材质",GUILayout.Width(100));
			
			GUILayout.BeginVertical();
			sri.showMaterial = GUILayout.Toggle(sri.showMaterial,"显示材质球");
			if(sri.showMaterial)
			{
				foreach(Material mat in sri.usedInMaterial)
				{
					if(null == mat)
						continue;
					if(GUILayout.Button(mat.name))
					{
						Selection.activeObject = mat;
					}
				}
			}
			
			GUILayout.EndVertical();
			if(GUILayout.Button("替换Shader"))
			{
				//调用另一个窗口
				//ShaderReplace.Init(m_selfShader,sri.usedInMaterial.ToArray(),false);// shaderReplaceWindow = ScriptableObject.CreateInstance<ShaderReplace>();
				//shaderReplaceWindow.Init();
			}
			GUILayout.EndHorizontal ();
		}
		GUILayout.EndScrollView ();
	}
	
	private void HierarchyWindow()
	{
		
	}
	private void LoadResources()
	{
		m_materials = null;
		m_selfShader = null;
		
		//获取自定义shader和材质
		List<Material> materialList = new List<Material> ();
		var materialGuids = AssetDatabase.FindAssets("t:Material", null );
        EditorUtility.DisplayProgressBar("Hold On...", "搜索所有材质", 0.1f);
		foreach( var id in materialGuids )
		{
			Material ctrller = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(id), typeof(Material)) as Material;
			materialList.Add(ctrller);
		}
		m_materials = materialList.ToArray();

        EditorUtility.DisplayProgressBar("Hold On...", "搜索所有Shader", 0.2f);
        List<Shader> shaderlist = new List<Shader> ();
		var shaderGuids = AssetDatabase.FindAssets("t:Shader", null );
		foreach( var id in shaderGuids )
		{
			Shader ctrller = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(id), typeof(Shader)) as Shader;
			shaderlist.Add(ctrller);
		}
		m_selfShader = shaderlist.ToArray ();
        EditorUtility.DisplayProgressBar("Hold On...", "开始分析资源", 0.3f);
        CheckResources ();
        EditorUtility.ClearProgressBar();
    }
	
	private void CheckResources()
	{
		shaderDetailList.Clear ();
		
		List<Shader> ShaderList = new List<Shader> ();
		//添加所有自定义shader
		for(int i=0; i<m_selfShader.Length; i++)
		{
			if(null == m_selfShader[i])
				continue;
			
			if(!ShaderList.Contains(m_selfShader[i]))
			{
				ShaderList.Add(m_selfShader[i]);
			}
		}
		//添加所有材质球用到的shader
		for(int i=0; i<m_materials.Length; i++)
		{
			if(null == m_materials[i])
				continue;
			
			if(!ShaderList.Contains(m_materials[i].shader))
			{
				ShaderList.Add(m_materials[i].shader);
			}
		}
		
		//构建Shader列表（包括内置、非内置）
		foreach(Shader shader in ShaderList)
		{
			ShaderReferenceInMaterial sri = new ShaderReferenceInMaterial();
			sri.shader = shader;
			for(int i=0; i<m_materials.Length; i++)
			{
				if(null == m_materials[i])
					continue;
				
				if(shader == m_materials[i].shader)
				{
					sri.materialCount += 1;
					sri.usedInMaterial.Add(m_materials[i]);
				}
			}
			shaderDetailList.Add(sri);
		}
		
		//标记shader类型
		foreach(ShaderReferenceInMaterial sri in shaderDetailList)
		{
			for(int i=0; i<m_selfShader.Length; i++)
			{
				if(null == m_selfShader[i])
					continue;
				
				if(sri.shader != m_selfShader[i])
					sri.shaderType = 0; 
				if(sri.shader.name.Contains("Hidden"))
					sri.shaderType = 1;
				else 
					sri.shaderType = 2;
			}
		}
	}
	
	void OnDestroy()
	{
		//ShaderReplace.ClearData ();
		//ShaderReplace.GetWindow (typeof(ShaderReplace)).Close ();
	}
}




