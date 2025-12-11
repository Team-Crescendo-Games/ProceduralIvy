using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamCrescendo.ProceduralIvy
{
    public class EditorMeshBuilder : ScriptableObject
    {
        public InfoPool infoPool;

        //La malla final de la enredadera en su conjunto
        public Mesh ivyMesh;

        //Datos de la malla
        public Vector3[] verts;

        //lista de materiales de las hojas
        public List<Material> leavesMaterials;

        //Esto es solo para debugear, para hacer una representación 
        public Rect[] uv2Rects = new Rect[0];

        //Booleano para saber si están inicializadas las estructuras para las hojas, así no intenta construir la geometría sin tener lo necesario
        public bool leavesDataInitialized;

        //ángulo para la generación de cada ring
        private float angle;

        //Diccionario usado en la generación de UVs de lightmap
        private readonly Dictionary<int, int[]> branchesLeavesIndices = new();

        private Vector3[] normals;

        //Triángulos de las ramas
        private int[] trisBranches;

        //Triángulos de las hojas, divididos según el material de cada tipo de hoja para hacer las submeshes. Se podría hacer con arrays, pero bueno, me permito la licencia dada la complejidad
        private List<List<int>> trisLeaves;

        //Aquí metemos qué tipos de hojas corresponden a cada material
        public List<List<int>> typesByMat;
        private Vector2[] uvs;
        private Color[] vColor;

        //Aquí se inicializan las estructuras de las hojas antes de empezar a generarse la enredadera y la geometría
        public void InitLeavesData()
        {
            if (infoPool.ivyContainer.ivyGO)
            {
                infoPool.meshBuilder.typesByMat = new List<List<int>>();
                infoPool.meshBuilder.leavesMaterials = new List<Material>();

                if (infoPool.ivyParameters.generateLeaves)
                {
                    //revisamos los materiales repetidos dentro de los prefabs
                    for (var i = 0; i < infoPool.ivyParameters.leavesPrefabs.Length; i++)
                    {
                        var materialExists = false;
                        for (var m = 0; m < infoPool.meshBuilder.leavesMaterials.Count; m++)
                            if (infoPool.meshBuilder.leavesMaterials[m] == infoPool.ivyParameters.leavesPrefabs[i]
                                    .GetComponent<MeshRenderer>().sharedMaterial)
                            {
                                infoPool.meshBuilder.typesByMat[m].Add(i);
                                materialExists = true;
                            }

                        if (!materialExists)
                        {
                            infoPool.meshBuilder.leavesMaterials.Add(infoPool.ivyParameters.leavesPrefabs[i]
                                .GetComponent<MeshRenderer>().sharedMaterial);
                            infoPool.meshBuilder.typesByMat.Add(new List<int>());
                            infoPool.meshBuilder.typesByMat[infoPool.meshBuilder.typesByMat.Count - 1].Add(i);
                        }
                    }

                    //Asignamos los materiales al mesh renderer una vez recogidos de los prefabs
                    var materials = new Material[leavesMaterials.Count + 1];
                    for (var i = 0; i < materials.Length; i++)
                        if (i == 0)
                            materials[i] = infoPool.ivyContainer.ivyGO.GetComponent<MeshRenderer>().sharedMaterial;
                        else
                            materials[i] = infoPool.meshBuilder.leavesMaterials[i - 1];

                    infoPool.ivyContainer.ivyGO.GetComponent<MeshRenderer>().sharedMaterials = materials;
                }
                else
                {
                    infoPool.ivyContainer.ivyGO.GetComponent<MeshRenderer>().sharedMaterials = new Material[1]
                        { infoPool.ivyParameters.branchesMaterial };
                }

                leavesDataInitialized = true;
            }
        }

        public void Initialize()
        {
            //Reiniciamos los triángulos de las hojas en cada iteración
            infoPool.meshBuilder.trisLeaves = new List<List<int>>();
            for (var i = 0; i < infoPool.meshBuilder.leavesMaterials.Count; i++)
                infoPool.meshBuilder.trisLeaves.Add(new List<int>());

            //Reiniciamos la malla y definimos el número de materiales
            ivyMesh.Clear();
            if (infoPool.ivyParameters.buffer32Bits) ivyMesh.indexFormat = IndexFormat.UInt32;
            ivyMesh.name = "Ivy Mesh";
            ivyMesh.subMeshCount = leavesMaterials.Count + 1;
            //Y también el diccionario usado en la creación de las uvs de lightmap
            branchesLeavesIndices.Clear();

            //Estos contadores son para calcular cuantos huecos hacen falta en los arrays de vertices y tris
            var vertCount = 0;
            var triBranchesCount = 0;
            if (infoPool.ivyParameters.generateBranches)
                //Contamos los verts y tris necesarios y hacemos hueco en las arrays Por este lado las ramas
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                    {
                        vertCount += (infoPool.ivyContainer.branches[i].branchPoints.Count - 1) *
                            (infoPool.ivyParameters.sides + 1) + 1;
                        triBranchesCount +=
                            (infoPool.ivyContainer.branches[i].branchPoints.Count - 2) * infoPool.ivyParameters.sides *
                            2 * 3 + infoPool.ivyParameters.sides * 3;
                    }

            if (infoPool.ivyParameters.generateLeaves && infoPool.ivyParameters.leavesPrefabs.Length > 0)
                //Y por este las hojas, dependiendo de la malla de cada prefab
                for (var i = 0; i < infoPool.ivyContainer.branches.Count; i++)
                    if (infoPool.ivyContainer.branches[i].branchPoints.Count > 1)
                        for (var j = 0; j < infoPool.ivyContainer.branches[i].leaves.Count; j++)
                        {
                            var currentBranch = infoPool.ivyContainer.branches[i];
                            //BranchPoint currentBranchPoint = infoPool.ivyContainer.branches[i].branchPoints[j];
                            var leafMeshFilter = infoPool.ivyParameters
                                .leavesPrefabs[currentBranch.leaves[j].chosenLeave].GetComponent<MeshFilter>();
                            vertCount += leafMeshFilter.sharedMesh.vertexCount;
                            /*for (int p = 0; p < currentBranchPoint.leaves.Count; p++)
                            {
                                vertCount += infoPool.ivyParameters.leavesPrefabs[currentBranchPoint.leaves[p].chosenLeave].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                            }*/
                        }

            //creamos las arrays para todos los datos de la malla (salvo los triángulos de las hojas que se van añadiendo al vuelo, pues son una listas)
            verts = new Vector3[vertCount];
            normals = new Vector3[vertCount];
            uvs = new Vector2[vertCount];
            vColor = new Color[vertCount];
            trisBranches = new int[Mathf.Max(triBranchesCount, 0)];
            //Calculamos el ángulo y tal
            if (!infoPool.ivyParameters.halfgeom)
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides;
            else
                angle = Mathf.Rad2Deg * 2 * Mathf.PI / infoPool.ivyParameters.sides / 2;
        }

        //Aquí se construyen las hojas, este método es llamado rama a rama
        private void BuildLeaves(int b, ref int vertCount)
        {
            Mesh chosenLeaveMesh;
            //Se recorren los materiales
            for (var i = 0; i < leavesMaterials.Count; i++)
            {
                Random.InitState(b + infoPool.ivyParameters.randomSeed + i);

                for (var j = 0; j < infoPool.ivyContainer.branches[b].leaves.Count; j++)
                {
                    var currentLeaf = infoPool.ivyContainer.branches[b].leaves[j];


                    //Ahora vemos si para el material que estamos iterando, le corresponde el tipo de hoja que tenemos en este punto
                    if (typesByMat[i].Contains(currentLeaf.chosenLeave))
                    {
                        currentLeaf.verticesLeaves = new List<RTVertexData>();
                        //Y vemos qué tipo de hoja corresponde a cada punto a cogemos esa malla
                        chosenLeaveMesh = infoPool.ivyParameters.leavesPrefabs[currentLeaf.chosenLeave]
                            .GetComponent<MeshFilter>().sharedMesh;
                        //definimos el vértice por el que tenemos que empezar a escribir en el array
                        var firstVertex = vertCount;
                        Vector3 left, forward;
                        Quaternion quat;
                        //Aquí cálculos de orientación en función de las opciones de rotación
                        if (!infoPool.ivyParameters.globalOrientation)
                        {
                            forward = currentLeaf.lpForward;
                            left = currentLeaf.left;
                            //left = Vector3.Cross(currentLeaf.lpForward, currentLeaf.lpUpward);
                        }
                        else
                        {
                            forward = infoPool.ivyParameters.globalRotation;
                            left = Vector3.Normalize(Vector3.Cross(infoPool.ivyParameters.globalRotation,
                                currentLeaf.lpUpward));
                        }
                        //Y aplicamos la rotación

                        quat = Quaternion.LookRotation(currentLeaf.lpUpward, forward);
                        quat = Quaternion.AngleAxis(infoPool.ivyParameters.rotation.x, left) *
                               Quaternion.AngleAxis(infoPool.ivyParameters.rotation.y, currentLeaf.lpUpward) *
                               Quaternion.AngleAxis(infoPool.ivyParameters.rotation.z, forward) * quat;
                        quat =
                            Quaternion.AngleAxis(
                                Random.Range(-infoPool.ivyParameters.randomRotation.x,
                                    infoPool.ivyParameters.randomRotation.x), left) *
                            Quaternion.AngleAxis(
                                Random.Range(-infoPool.ivyParameters.randomRotation.y,
                                    infoPool.ivyParameters.randomRotation.y), currentLeaf.lpUpward) *
                            Quaternion.AngleAxis(
                                Random.Range(-infoPool.ivyParameters.randomRotation.z,
                                    infoPool.ivyParameters.randomRotation.z), forward) * quat;
                        quat = currentLeaf.forwarRot * quat;


                        //Aquí la escala, que es facilita, incluyendo el tip influence
                        var scale = Random.Range(infoPool.ivyParameters.minScale, infoPool.ivyParameters.maxScale);
                        currentLeaf.leafScale = scale;


                        scale *= Mathf.InverseLerp(infoPool.ivyContainer.branches[b].totalLenght,
                            infoPool.ivyContainer.branches[b].totalLenght - infoPool.ivyParameters.tipInfluence,
                            currentLeaf.lpLength);


                        /*******************/
                        currentLeaf.leafRotation = quat;
                        currentLeaf.dstScale = scale;
                        /*******************/


                        //Metemos los triángulos correspondientes en el array correspondiente al material que estamos iterando
                        for (var t = 0; t < chosenLeaveMesh.triangles.Length; t++)
                        {
                            var triangle = chosenLeaveMesh.triangles[t] + vertCount;
                            trisLeaves[i].Add(triangle);
                        }

                        //ylos vértices, normales y uvs, aplicando las transformaciones pertinentes, actualizando el contador para la siguiente iteración saber por dónde vamos
                        for (var v = 0; v < chosenLeaveMesh.vertexCount; v++)
                        {
                            var offset = left * infoPool.ivyParameters.offset.x +
                                         currentLeaf.lpUpward * infoPool.ivyParameters.offset.y +
                                         currentLeaf.lpForward * infoPool.ivyParameters.offset.z;

                            verts[vertCount] = quat * chosenLeaveMesh.vertices[v] * scale + currentLeaf.point + offset;
                            normals[vertCount] = quat * chosenLeaveMesh.normals[v];
                            uvs[vertCount] = chosenLeaveMesh.uv[v];
                            vColor[vertCount] = chosenLeaveMesh.colors[v];

                            normals[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                                 normals[vertCount];
                            verts[vertCount] -= infoPool.ivyContainer.ivyGO.transform.position;
                            verts[vertCount] = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                               verts[vertCount];


                            var vertexData = new RTVertexData(verts[vertCount], normals[vertCount], uvs[vertCount],
                                Vector2.zero, vColor[vertCount]);
                            currentLeaf.verticesLeaves.Add(vertexData);

                            currentLeaf.leafCenter = currentLeaf.point - infoPool.ivyContainer.ivyGO.transform.position;
                            currentLeaf.leafCenter =
                                Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                currentLeaf.leafCenter;

                            vertCount++;
                        }

                        //escribimos en el diccionario el index por donde nos hemos quedado  para después poder
                        //transformar las uvs de cada elemento acorde a su dimensión real
                        var fromTo = new int[2] { firstVertex, vertCount - 1 };
                        branchesLeavesIndices.Add(branchesLeavesIndices.Count, fromTo);
                    }


                    //for (int p = 0; p < currentBranchPoint.leaves.Count; p++)
                    //{

                    //}
                }


                /*//Después por cada material, recorremos los puntos de la rama
                for (int j = 0; j < infoPool.ivyContainer.branches[b].branchPoints.Count; j++) {






                }*/
            }
        }

        public void BuildGeometry()
        {
            if (leavesDataInitialized)
            {
                //Lo primero inicializar
                Initialize();
                //Estos contadores nos servirán para saber por dónde vamos calculándo vértices y triángulos, ya q lo calcularemos todo a buco, sin ir por ramas
                var vertCount = 0;
                var triBranchesCount = 0;

                //Recorremos cada rama y definimos el primer vértice que tenemos que escribir del array, recogido del vertcount actualizado en la iteración anterior
                for (var b = 0; b < infoPool.ivyContainer.branches.Count; b++)
                {
                    var firstVertex = vertCount;
                    Random.InitState(b + infoPool.ivyParameters.randomSeed);
                    if (infoPool.ivyContainer.branches[b].branchPoints.Count > 1)
                    {
                        //En este contador guardaremos cuántos vértices tiene la rama actual, para en la siguiente tenerlo en cuenta y saber qué vértices hay que 
                        //escribir
                        var lastVertCount = 0;
                        //Recorremos cada punto de la rama hasta el penúltimo
                        for (var p = 0; p < infoPool.ivyContainer.branches[b].branchPoints.Count; p++)
                        {
                            //Si no es el último punto, calculamos el ring de vértices


                            var branchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
                            branchPoint.verticesLoop = new List<RTVertexData>();


                            var centerVertexPosition =
                                branchPoint.point - infoPool.ivyContainer.ivyGO.transform.position;
                            centerVertexPosition = Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                                   centerVertexPosition;
                            var radius = CalculateRadius(branchPoint.length,
                                infoPool.ivyContainer.branches[b].totalLenght);

                            branchPoint.radius = radius;

                            if (p != infoPool.ivyContainer.branches[b].branchPoints.Count - 1)
                            {
                                //En este array, el método nos mete en el index 0 el firstvector, y en el index 1 el axis de rotación del ring
                                var vectors = CalculateVectors(infoPool.ivyContainer.branches[b].branchPoints[p].point,
                                    p, b);

                                branchPoint.firstVector = vectors[0];
                                branchPoint.axis = vectors[1];


                                for (var v = 0; v < infoPool.ivyParameters.sides + 1; v++)
                                    if (infoPool.ivyParameters.generateBranches)
                                    {
                                        //BranchPoint branchPoint = infoPool.ivyContainer.branches[b].branchPoints[p];
                                        var tipInfluence = GetTipInfluence(branchPoint.length,
                                            infoPool.ivyContainer.branches[b].totalLenght);
                                        infoPool.ivyContainer.branches[b].branchPoints[p].radius = radius;

                                        var quat = Quaternion.AngleAxis(angle * v, vectors[1]);
                                        var direction = quat * vectors[0];
                                        //Excepción para el cálculo de normales si tenemos el caso de media geometría y 1 lado
                                        if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
                                            normals[vertCount] = -infoPool.ivyContainer.branches[b].branchPoints[p]
                                                .grabVector;
                                        else
                                            normals[vertCount] = direction;

                                        var vertexForRuntime = direction * radius + centerVertexPosition;

                                        verts[vertCount] = direction * radius * tipInfluence +
                                                           infoPool.ivyContainer.branches[b].branchPoints[p].point;
                                        verts[vertCount] -= infoPool.ivyContainer.ivyGO.transform.position;
                                        verts[vertCount] =
                                            Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                            verts[vertCount];


                                        uvs[vertCount] =
                                            new Vector2(
                                                branchPoint.length * infoPool.ivyParameters.uvScale.y +
                                                infoPool.ivyParameters.uvOffset.y - infoPool.ivyParameters.stepSize,
                                                1f / infoPool.ivyParameters.sides * v *
                                                infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

                                        normals[vertCount] =
                                            Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                            normals[vertCount];


                                        var vertexData = new RTVertexData(vertexForRuntime, normals[vertCount],
                                            uvs[vertCount], Vector2.zero, vColor[vertCount]);
                                        branchPoint.verticesLoop.Add(vertexData);


                                        //Vamos actualizando estos contadores para en la siguiente pasada saber por dónde íbamos escribiendo en el array
                                        vertCount++;
                                        lastVertCount++;
                                    }
                            }
                            //Si es el último punto, en lugar de calcular el ring, usamos el último punto para escribir el último vértice de esta rama
                            else
                            {
                                if (infoPool.ivyParameters.generateBranches)
                                {
                                    verts[vertCount] = infoPool.ivyContainer.branches[b].branchPoints[p].point;
                                    //Corrección de espacio local
                                    verts[vertCount] -= infoPool.ivyContainer.ivyGO.transform.position;
                                    verts[vertCount] =
                                        Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                        verts[vertCount];
                                    //verts[vertCount] = centerVertexPosition;


                                    //Excepción para las normales en el caso de media geometría y 1 solo lado
                                    if (infoPool.ivyParameters.halfgeom && infoPool.ivyParameters.sides == 1)
                                        normals[vertCount] =
                                            -infoPool.ivyContainer.branches[b].branchPoints[p].grabVector;
                                    else
                                        normals[vertCount] = Vector3.Normalize(
                                            infoPool.ivyContainer.branches[b].branchPoints[p].point -
                                            infoPool.ivyContainer.branches[b].branchPoints[p - 1].point);
                                    uvs[vertCount] = new Vector2(
                                        infoPool.ivyContainer.branches[b].totalLenght *
                                        infoPool.ivyParameters.uvScale.y + infoPool.ivyParameters.uvOffset.y,
                                        0.5f * infoPool.ivyParameters.uvScale.x + infoPool.ivyParameters.uvOffset.x);

                                    normals[vertCount] =
                                        Quaternion.Inverse(infoPool.ivyContainer.ivyGO.transform.rotation) *
                                        normals[vertCount];


                                    var vertexForRuntime = centerVertexPosition;


                                    var vertexData = new RTVertexData(vertexForRuntime, normals[vertCount],
                                        uvs[vertCount], Vector2.zero, vColor[vertCount]);
                                    branchPoint.verticesLoop.Add(vertexData);


                                    //Vamos actualizando estos contadores para en la siguiente pasada saber por dónde íbamos escribiendo en el array
                                    vertCount++;
                                    lastVertCount++;


                                    //Y después de poner el último vértice, triangulamos
                                    TriangulateBranch(b, ref triBranchesCount, vertCount, lastVertCount);
                                }
                            }
                        }
                    }

                    //escribimos en el diccionario el index por donde nos hemos quedado  para después poder
                    //transformar las uvs de cada elemento acorde a su dimensión real
                    var fromTo = new int[2] { firstVertex, vertCount - 1 };
                    branchesLeavesIndices.Add(branchesLeavesIndices.Count, fromTo);


                    if (infoPool.ivyParameters.generateLeaves)
                        //infoPool.ivyContainer.branches[b].ClearRuntimeVerticesLeaves();
                        BuildLeaves(b, ref vertCount);
                }

                //Y pasamos los vértices y tris a la malla
                ivyMesh.vertices = verts;
                ivyMesh.normals = normals;
                ivyMesh.uv = uvs;
                ivyMesh.colors = vColor;
                ivyMesh.SetTriangles(trisBranches, 0);
                //Por cada material, metemos los triángulos de hojas al submesh correspondiente
                for (var i = 0; i < leavesMaterials.Count; i++) ivyMesh.SetTriangles(trisLeaves[i], i + 1);
                ivyMesh.RecalculateTangents();
                ivyMesh.RecalculateBounds();
            }
        }


        //Con esto se calculan vectores para el cálculo de cada ring
        private Vector3[] CalculateVectors(Vector3 branchPoint, int p, int b)
        {
            //Declaramos el firstvector del ring, el eje sobre el que vamos a rotar, la rotación de cada vértice
            Vector3 firstVector;
            Vector3 axis;
            //para el primer punto de la primera rama definimos las variables
            if (b == 0 && p == 0)
            {
                axis = infoPool.ivyContainer.ivyGO.transform.up;
                //Excepción para media geometría, para que el arco salga bien alineado respecto al suelo
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector = infoPool.ivyContainer.firstVertexVector;
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) * infoPool.ivyContainer.firstVertexVector;
            }
            //para todo lo demas, tendremos como eje una interpolación del segmento anterior y el siguiente al punto en cuestión, y como firstvector una proyección del grabvector sobre el plano del eje
            else
            {
                if (p == 0)
                    axis = infoPool.ivyContainer.branches[b].branchPoints[p + 1].point -
                           infoPool.ivyContainer.branches[b].branchPoints[p].point;
                else
                    axis = Vector3.Normalize(Vector3.Lerp(
                        infoPool.ivyContainer.branches[b].branchPoints[p].point -
                        infoPool.ivyContainer.branches[b].branchPoints[p - 1].point,
                        infoPool.ivyContainer.branches[b].branchPoints[p + 1].point -
                        infoPool.ivyContainer.branches[b].branchPoints[p].point, 0.5f));
                if (!infoPool.ivyParameters.halfgeom)
                    firstVector =
                        Vector3.Normalize(
                            Vector3.ProjectOnPlane(infoPool.ivyContainer.branches[b].branchPoints[p].grabVector, axis));
                else
                    firstVector = Quaternion.AngleAxis(90f, axis) * Vector3.Normalize(
                        Vector3.ProjectOnPlane(infoPool.ivyContainer.branches[b].branchPoints[p].grabVector, axis));
            }

            //retornamos los vectores que hemos calculado

            return new Vector3[2] { firstVector, axis };
        }

        //Cálculo del  radio según la distancia recorrida por la rama en ese punto, no es complejo, paso de explicarlo
        private float CalculateRadius(float lenght, float totalLenght)
        {
            var value = (Mathf.Sin(lenght * infoPool.ivyParameters.radiusVarFreq +
                                   infoPool.ivyParameters.radiusVarOffset) + 1f) / 2f;
            var radius = Mathf.Lerp(infoPool.ivyParameters.minRadius, infoPool.ivyParameters.maxRadius, value);

            //No recuerdo aquí por qué puse el -0.1f este :S
            /*if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence) {
                radius *= Mathf.InverseLerp (totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);
            }*/
            return radius;
        }

        private float GetTipInfluence(float lenght, float totalLenght)
        {
            var res = 1.0f;

            if (lenght - 0.1f >= totalLenght - infoPool.ivyParameters.tipInfluence)
                res = Mathf.InverseLerp(totalLenght, totalLenght - infoPool.ivyParameters.tipInfluence, lenght - 0.1f);

            return res;
        }

        //Algoritmo de triangulación, usamos el número de puntos que tiene la rama, el contador de triángulos global, contador de vértices global, y el número de vértices que tenía la última rama.
        private void TriangulateBranch(int b, ref int triCount, int vertCount, int lastVertCount)
        {
            //Hacemos una ronda por cada punto de la rama hasta el penúltimo
            for (var round = 0; round < infoPool.ivyContainer.branches[b].branchPoints.Count - 2; round++)
                //Y por cada ronda hacemos una pasada a cada lado de la rama
            for (var i = 0; i < infoPool.ivyParameters.sides; i++)
            {
                //Y vamos asignando índices a cada hueco del array de tris con el algoritmo. Para escribir en los huecos correctos sumamos el total de vértices que hay y 
                //restamos los de la última ráma, así empezamos en el lugar correcto
                trisBranches[triCount] = i + round * (infoPool.ivyParameters.sides + 1) + vertCount - lastVertCount;
                trisBranches[triCount + 1] =
                    i + round * (infoPool.ivyParameters.sides + 1) + 1 + vertCount - lastVertCount;
                trisBranches[triCount + 2] = i + round * (infoPool.ivyParameters.sides + 1) +
                    infoPool.ivyParameters.sides + 1 + vertCount - lastVertCount;

                trisBranches[triCount + 3] =
                    i + round * (infoPool.ivyParameters.sides + 1) + 1 + vertCount - lastVertCount;
                trisBranches[triCount + 4] = i + round * (infoPool.ivyParameters.sides + 1) +
                    infoPool.ivyParameters.sides + 2 + vertCount - lastVertCount;
                trisBranches[triCount + 5] = i + round * (infoPool.ivyParameters.sides + 1) +
                    infoPool.ivyParameters.sides + 1 + vertCount - lastVertCount;
                triCount += 6;
            }

            //Aquí vienen los triángulos del capuchón
            for (int t = 0, c = 0; t < infoPool.ivyParameters.sides * 3; t += 3, c++)
            {
                trisBranches[triCount] = vertCount - 1;
                trisBranches[triCount + 1] = vertCount - 3 - c;
                trisBranches[triCount + 2] = vertCount - 2 - c;
                triCount += 3;
            }
        }

#if UNITY_EDITOR
        public void GenerateLMUVs()
        {
            if (ivyMesh) Unwrapping.GenerateSecondaryUVSet(ivyMesh);
        }
#endif
    }
}