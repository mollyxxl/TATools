using UnityEditor;
namespace XXL
{
    /*
     *  XXL相关菜单选项
     *   */
    public class XXLMenuHelper
    {
        [MenuItem("XXL/Debug工具/Gizmos中显示TBN方向", false, 101)]
        public static void GenerateDirGUIDRefCache1()
        {
            var selection = Selection.activeGameObject;
            if (selection != null)
            {
                var tbn = selection.GetComponent<ModelTBNShow>();
                if (!tbn)
                    selection.AddComponent<ModelTBNShow>();
                else
                {
                    EditorUtility.DisplayDialog("提示", "ModelTBNShow已经存在,直接显示", "ok");
                    tbn.enabled = true;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请选择MeshRender对象进行操作", "ok");
            }
        }
    }
}