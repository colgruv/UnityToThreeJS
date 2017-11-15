using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


public class ModelExporter : EditorWindow
{
    //private StreamWriter m_JSONWriter;
    private StreamWriter m_XMLWriter;
    private Transform[] m_Selections;
    private string m_Root;
    private List<Material> m_Materials;
    private List<Transform> m_SubMeshes;

    private float m_Progress;
    private int m_ProcessesCompleted;
    private int m_TotalProcesses;
    private bool m_Generating = false;

    [MenuItem ("Window/Export for ThreeJS")]
    static void Init()
    {
        ModelExporter window = (ModelExporter)EditorWindow.GetWindow(typeof(ModelExporter));
    }
	
	void OnGUI()
    {
        m_Selections = Selection.GetTransforms(SelectionMode.Editable);

        // Only allow button if we have selected something
        GUI.enabled = (m_Selections != null && m_Selections.Length > 0);

        if (GUILayout.Button("Export Selection"))
        {
            string path = EditorUtility.SaveFilePanel("Export Model for ThreeJS", "", m_Selections[0].name + ".xml", "xml");
            m_Root = path.Substring(0, path.LastIndexOf("/") + 1);

            if (path.Length > 0)
                beginExportModel(path);
        }

        if (m_Generating)
        {
            if (m_TotalProcesses == 0)
            {
                Debug.LogError("No processes to run; canceling export.");
                m_Generating = false;
                return;
            }

            EditorUtility.DisplayProgressBar("Generating Model Data", "Current Mesh: " + m_SubMeshes[m_ProcessesCompleted].name, m_Progress);
            generateSubMeshXML(m_ProcessesCompleted);

            if (m_ProcessesCompleted == m_TotalProcesses)
            {
                m_XMLWriter.WriteLine("</Mesh>\n");

                m_XMLWriter.WriteLine("<Materials>");
                writeMaterials();
                m_XMLWriter.WriteLine("</Materials>");
                m_XMLWriter.Write("</Model>");

                m_XMLWriter.Close();
                m_Generating = false;
                EditorUtility.ClearProgressBar();
            }
        }
    }

    private void beginExportModel(string _outputUrl)
    {
        m_Progress = 0;
        m_ProcessesCompleted = 0;
        m_TotalProcesses = 0;

        m_SubMeshes = new List<Transform>();
        m_XMLWriter = new StreamWriter(_outputUrl);
        m_Materials = new List<Material>();

        // Recursively generate list of processes so that we can track progress
        recurseChild(m_Selections[0]);

        m_XMLWriter.WriteLine("<Model>");
        m_XMLWriter.WriteLine("<Mesh>");

        m_Generating = true;
    }

    private void recurseChild(Transform _transform)
    {
        if (_transform.GetComponent<MeshFilter>())
        {
            //Debug.Log(_transform.name);

            m_TotalProcesses++;
            m_SubMeshes.Add(_transform);
        }

        if (_transform.GetComponent<MeshRenderer>())
        {
            if (!m_Materials.Contains(_transform.GetComponent<MeshRenderer>().sharedMaterials[0]))
            {
                m_Materials.Add(_transform.GetComponent<MeshRenderer>().sharedMaterials[0]);
            }
        }

        for (int i = 0; i < _transform.childCount; i++)
        {
            recurseChild(_transform.GetChild(i));
        }
    }

    private void generateSubMeshXML(int _index)
    {
        Transform currentSubMesh = m_SubMeshes[_index];

        m_XMLWriter.WriteLine("\t<Submesh mat=\"" + currentSubMesh.GetComponent<MeshRenderer>().sharedMaterials[0].name + "\">");
        generateSubMeshJSON(currentSubMesh);
        m_XMLWriter.WriteLine("\t</Submesh>");

        m_ProcessesCompleted++;
        m_Progress = (float)m_ProcessesCompleted / (float)m_TotalProcesses;
    }

    private void generateSubMeshJSON(Transform _trans)
    {
        Mesh _mesh = _trans.GetComponent<MeshFilter>().sharedMesh;
        string jsonOut;

        // Open submesh
        m_XMLWriter.Write("\t{\n");

        // Vertices
        jsonOut = "\t\t\"vertices\":[";
        foreach (Vector3 vertex in _mesh.vertices)
        {
            // Write each chunk as we confirm subsequent data to keep string size small
            //Debug.Log("Writing chunk to json: " + jsonOut);
            m_XMLWriter.Write(jsonOut);
            jsonOut = "";

            Vector3 currentVertex = vertex;

            // Apply transformation to individual vertex
            currentVertex.Scale(_trans.lossyScale);
            currentVertex = Quaternion.Euler(_trans.eulerAngles) * currentVertex;
            currentVertex += _trans.position;

            jsonOut += currentVertex.x.ToString() + "," + currentVertex.y.ToString() + "," + currentVertex.z.ToString() + ",";
        }
        jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma
        jsonOut += "],\n";
        m_XMLWriter.Write(jsonOut);

        // Faces
        jsonOut = "\t\t\"faces\":[";
        for(int i = 0; i < _mesh.triangles.Length; i+= 3)
        {
            // Write each chunk as we confirm subsequent data to keep string size small
            //Debug.Log("Writing chunk to json: " + jsonOut);
            m_XMLWriter.Write(jsonOut);
            jsonOut = "";

            jsonOut += "40," + _mesh.triangles[i].ToString() + "," + _mesh.triangles[i+1].ToString() + "," + _mesh.triangles[i+2].ToString() + ","
                + _mesh.triangles[i].ToString() + "," + _mesh.triangles[i + 1].ToString() + "," + _mesh.triangles[i + 2].ToString() + ","
                + _mesh.triangles[i].ToString() + "," + _mesh.triangles[i + 1].ToString() + "," + _mesh.triangles[i + 2].ToString() + ",";
        }
        jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma
        jsonOut += "],\n";
        m_XMLWriter.Write(jsonOut);

        // Metadata
        m_XMLWriter.Write("\t\t\"metadata\":{\n");
        m_XMLWriter.Write("\t\t\t\"vertices\":" + _mesh.vertexCount + ",\n");
        m_XMLWriter.Write("\t\t\t\"faces\":" + _mesh.triangles.Length/3 + ",\n");
        m_XMLWriter.Write("\t\t\t\"generator\":\"io_three\",\n");
        m_XMLWriter.Write("\t\t\t\"type\":\"Geometry\",\n");
        m_XMLWriter.Write("\t\t\t\"normals\":" + _mesh.normals.Length + ",\n");
        m_XMLWriter.Write("\t\t\t\"version\":3,\n");
        m_XMLWriter.Write("\t\t\t\"uvs\":1\n");
        m_XMLWriter.Write("\t\t},\n");

        // UVs
        jsonOut = "\t\t\"uvs\":[[";
        foreach (Vector2 uv in _mesh.uv)
        {
            // Write each chunk as we confirm subsequent data to keep string size small
            //Debug.Log("Writing chunk to json: " + jsonOut);
            m_XMLWriter.Write(jsonOut);
            jsonOut = "";

            jsonOut += uv.x.ToString() + "," + uv.y.ToString() + ",";
        }
        jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma

        jsonOut += "]],\n";
        m_XMLWriter.Write(jsonOut);

        // Normals
        jsonOut = "\t\t\"normals\":[";
        foreach (Vector3 normal in _mesh.normals)
        {
            // Write each chunk as we confirm subsequent data to keep string size small
            //Debug.Log("Writing chunk to json: " + jsonOut);
            m_XMLWriter.Write(jsonOut);
            jsonOut = "";

            jsonOut += normal.x.ToString() + "," + normal.y.ToString() + "," + normal.z.ToString() + ",";
        }
        jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma
        jsonOut += "]\n";
        m_XMLWriter.Write(jsonOut);

        // Close submesh
        m_XMLWriter.Write("\t}\n");
    }

    private void writeMaterials()
    {
        foreach (Material mat in m_Materials)
        { 
            m_XMLWriter.Write("\t<Material id=\"" + mat.name + "\" ");
            m_XMLWriter.Write("color=\"0x" + ColorUtility.ToHtmlStringRGB(mat.color).ToLower() + "\" ");

            // Diffuse texture URL
            Texture diffuse = mat.GetTexture("_MainTex");
            if (diffuse)
            {
                m_XMLWriter.Write("diffuse=\"" + diffuse.name + ".png\" ");
            }

            // Normal map URL
            Texture normal = mat.GetTexture("_BumpMap");
            if (normal)
            {
                m_XMLWriter.Write("normal=\"" + normal.name + ".png\" ");
            }

            // Metalness map
            Texture metalness = mat.GetTexture("_MetallicGlossMap");
            if (metalness)
            {
                m_XMLWriter.Write("metalnessMap=\"" + metalness.name + ".png\" ");
            }

            Texture roughness = mat.GetTexture("_SpecGlossMap");
            if (roughness)
            {
                m_XMLWriter.Write("roughnessMap=\"" + roughness.name + ".png\" ");
            }

            Texture alpha = mat.GetTexture("_DetailMask");
            if (alpha)
            {
                m_XMLWriter.Write("alphaMap=\"" + alpha.name + ".png\" ");
            }

            // Occlusion map
            Texture occlusion = mat.GetTexture("_OcclusionMap");
            if (occlusion)
            {
                m_XMLWriter.Write("occlusion=\"" + occlusion.name + ".png\" ");
            }

            //m_XMLWriter.Write("metalness=\"" + mat.GetFloat("_Metallic") + "\" ");
            //m_XMLWriter.Write("smoothness=\"" + mat.GetFloat("_GlossMapScale") + "\"");

            m_XMLWriter.WriteLine("/>");
        }
    }

    /*
    private Texture2D combineDetailMaps(Texture2D _aoMap = null, Texture2D _metalnessMap = null, Texture2D _smoothnessMap = null)
    {
        Vector2 dimensions = new Vector2();
        if (_aoMap)
            dimensions = new Vector2(_aoMap.width, _aoMap.height);
        else if (_metalnessMap)
            dimensions = new Vector2(_metalnessMap.width, _metalnessMap.height);
        else if (_smoothnessMap)
            dimensions = new Vector2(_smoothnessMap.width, _smoothnessMap.height);
        else
            return null;

        Texture2D result = new Texture2D((int)dimensions.x, (int)dimensions.y);

        Color[] aoPixels = new Color[0];
        Color[] metalnessPixels = new Color[0];
        Color[] smoothnessPixels = new Color[0];
        int numPixels = 0;

        if (_aoMap)
        {
            aoPixels = _aoMap.GetPixels();
            numPixels = aoPixels.Length;
        }
            
        if (_metalnessMap)
        {
            metalnessPixels = _metalnessMap.GetPixels();
            numPixels = metalnessPixels.Length;
        }
            
        if (_smoothnessMap)
        {
            smoothnessPixels = _smoothnessMap.GetPixels();
            numPixels = smoothnessPixels.Length;
        }

        for (int i = 0; i < numPixels; i++)
        {
            if (aoPixels.Length > i)
                aoPixels[i] *= new Vector4(1f, 0f, 0f, 0f);

            if (metalnessPixels.Length > i)
                metalnessPixels[i] *= new Vector4(0f, 1f, 0f, 0f);

            if (smoothnessPixels.Length > i)
                smoothnessPixels[i] *= new Vector4(0f, 0f, 1f, 0f);
        }


        return result;
    }
    */
}
