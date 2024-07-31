using Assets.Scripts.Extensions;
using Assets.Scripts;
using GLTF.Schema;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Networking;
using Assets.Scripts.Enums;
using Assets.Scripts.NodeProcessing;
using Assets.Scripts.Networking.Responses;
using Assets.Scripts.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Assets.Scripts.QualityMetrics;
using System.Text;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using Assets.Scripts.GameObjectScripts;
using UnityEngine.SceneManagement;

public class GLBStreamer : MonoBehaviour
{
    private StreamingSettings _settings;

    private GLBFile _glbFile;
    private string _uri;
    private GLBDownloader _downloader;
    private GLTFRoot _gltfRoot;
    private StreamingState _state;
    private Vector3 previousCameraPosition;
    private StreamingCycle _cycle;
    private bool _hasRenderedAll = false;
    private QualityMetricsTool _vmafUtility;
    private static int _currentFileNo = 1;
    private List<Task> _renderTasks;
    private Stopwatch _stopwatch;

    async void Start()
    {
        InitializeSettings();
        _stopwatch = Stopwatch.StartNew();
        _uri = $"http://{_settings.ServerPath}/{_settings.FileToStream.GetAttributeOfType<FileStringAttribute>().Value}";
        _downloader = new GLBDownloader(_uri);
        _renderTasks = new List<Task>();
        _glbFile = await _downloader.DownloadBasicGLBFile();
        var length = 28 + _glbFile.JSONSubheader.ChunkLength;
        Debug.Log($"Binary chunk length: " + _glbFile.BinaryDataSubheader.ChunkLength);

        _gltfRoot = await JsonUtils.DeserializeAsync(_glbFile.Json);
        InitializeState();


        RemoveOldScreenshots();
        TryLoadCameraTransform();

        if (_settings.MetricsMode)
        {
            await CalculateVMAF();
            return;
        }

        if (_settings.BulkDownloadMode)
        {
            await RenderBulkAsync();
            return;
        }

        //this.AddComponent<NodeDownloader>();


        await StartNewCycle(async () =>
        {
            //_renderTasks.Add(_cycle.TryRender());
            var shouldStartNewCycle = !_cycle.HasRenderedAllNodes();
            if (shouldStartNewCycle)
            {
                await _cycle.Execute();
            }
        },
        () => { });
    }



    private void TryLoadCameraTransform()
    {
        var camera = GameObject.Find("Main Camera");
        var sceneName = _settings.FileToStream.ToString();
        var pos = PlayerPrefs.GetString(sceneName + "_pos");
        var rot = PlayerPrefs.GetString(sceneName + "_rot");

        if (pos == string.Empty || rot == string.Empty)
        {
            TrySaveCameraTransform();
            return;
        }
        var splitPos = pos.Split(",");
        var posX = float.Parse(splitPos[0]);
        var posY = float.Parse(splitPos[1]);
        var posZ = float.Parse(splitPos[2]);

        var splitRot = rot.Split(",");
        var rotX = float.Parse(splitRot[0]);
        var rotY = float.Parse(splitRot[1]);
        var rotZ = float.Parse(splitRot[2]);
        var rotW = float.Parse(splitRot[3]);

        camera.transform.position = new Vector3(posX, posY, posZ);
        camera.transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
        Debug.Log($"Successfully loaded camera position {camera.transform.position}, rotation {camera.transform.rotation}");
    }

    private void TrySaveCameraTransform()
    {
        var sceneName = _settings.FileToStream.ToString();
        var pos = Camera.main.transform.position;
        var rot = Camera.main.transform.rotation;
        PlayerPrefs.SetString(sceneName + "_pos", $"{pos.x},{pos.y},{pos.z}");
        PlayerPrefs.SetString(sceneName + "_rot", $"{rot.x},{rot.y},{rot.z},{rot.w}");
        Debug.Log($"Successfully saved camera position {Camera.main.transform.position}, rotation {Camera.main.transform.rotation}");
    }

    private async Task RenderBulkAsync()
    {
        var response = await _downloader.DownloadFullAsync();
        var renderer = new GLTFastRenderer(transform);
        await renderer.RenderAsync(response.Data);
        _stopwatch.Stop();
        Debug.Log($"Rendered all nodes in {_stopwatch.ElapsedMilliseconds}ms!!");
    }



    private async Task StartNewCycle(Action downloadCallback, Action renderCallback)
    {
        _cycle = new StreamingCycle(downloadCallback);
        _cycle.OnRendered += () => renderCallback();
        await _cycle.Execute();
    }

    private void RemoveOldScreenshots()
    {
        System.IO.DirectoryInfo di = new DirectoryInfo(Application.persistentDataPath);
        foreach (FileInfo file in di.GetFiles())
        {
            if ((file.Extension == ".png" || file.Extension == ".jpg") && !IsFileLocked(file))
                file.Delete();
        }
    }

    private void InitializeSettings()
    {
        var settings = StreamingSettings.LoadFromPlayerPrefs();
        if (settings == null)
        {
            settings = StreamingSettings.DefaultSettings;
            settings.SaveToPlayerPrefs();
        }
        _settings = settings;
    }

    private void InitializeState()
    {
        _state = StreamingState.Instance;
        _state.GLBFile = _glbFile;
        _state.GLTFRoot = _gltfRoot;
        _state.ResourceUri = _uri;
        _state.RootTransform = this.transform.root;
        _state.NetworkSettings = new NetworkSettings()
        {
            Patience = _settings.Patience
        };
        _state.AverageDownloadSpeed = GLBDownloader.SpeedTracker.CalculateAverageSpeed();
        _state.NoLevelOfDetail = _settings.QuickRenderGeometry;
        _state.NodeBoundingBoxes = new Dictionary<ExtendedNode, NodeBoundingBox>();
        _state.BudgetStrategy = _settings.Strategy;
    }

    #region VMAF

    private async Task CalculateVMAF()
    {
        var screenShotPath = Application.persistentDataPath;
        _vmafUtility = new QualityMetricsTool(screenShotPath);
        var refFileName = "ref.png";
        _vmafUtility.ReferenceFileName = refFileName;
        var sw = Stopwatch.StartNew();
        var allData = await _downloader.DownloadGLBAsync();
        var completeResponse = (CompleteResponse)allData;
        sw.Stop();
        var speed = GLBDownloader.CalculateDownloadSpeed(sw.ElapsedMilliseconds, completeResponse.GetData().Length);
        GLBDownloader.SpeedTracker.AddDownloadSpeed(speed);
        Debug.Log($"Downloading full scene took {sw.ElapsedMilliseconds}ms");
        var renderer = new GLTFastRenderer(this.transform);
        await renderer.RenderAsync(completeResponse.GetData());
        // Take screenshot of entire scene
        StartCoroutine(TakeScreenshot(screenShotPath + "/" + refFileName, async () =>
        {
            _vmafUtility.AddScreenshotTime(refFileName, DateTime.Now);
            _stopwatch = Stopwatch.StartNew();

            foreach (Transform child in this.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            await StartNewCycle(async () =>
            {
                var shouldStartNewCycle = !_cycle.HasRenderedAllNodes();
                if (shouldStartNewCycle)
                {
                    await _cycle.Execute();
                }
            },
            () =>
            {
                var fileName = $"{(_currentFileNo++)}.png";
                _stopwatch.Stop();
                _vmafUtility.AddScreenshotTime(fileName, DateTime.Now);

                StartCoroutine(TakeScreenshot(screenShotPath + "/" + fileName, () =>
                {
                    _stopwatch.Start();
                }));
            });
        }));


    }




    IEnumerator TakeScreenshot(string filePath, Action callback)
    {

        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(filePath);
        yield return new WaitForEndOfFrame();
        callback();
    }

    #endregion


    void OnApplicationQuit()
    {
        try
        {
            PlayerPrefs.Save();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ms,ssim,msssim,vmaf,psnr");
            sb.AppendLine("0,0,0,0,0");
            var screenShotPath = Application.persistentDataPath;
            var metrics = _vmafUtility.CalculateMetrics();
            var referenceTime = _vmafUtility.GetScreenshotTime(_vmafUtility.ReferenceFileName);
            for (int i = 0; i < _currentFileNo - 1; i++)
            {
                var screenshotName = _vmafUtility.GetScreenshotName(i + 1);
                var metr = metrics.Frames[i].Metrics;
                var time = _vmafUtility.GetScreenshotTime(screenshotName);
                TimeSpan difference = time - referenceTime;
                double ms = difference.TotalMilliseconds;
                sb.AppendLine($"{ms},{metr.SSIM},{metr.MSSSIM},{metr.VMAF},{metr.PSNR}");
            }
            Debug.Log(sb.ToString());
            //Debug.Log(_vmafUtility.ExtractCSV());
        }
        catch (InvalidOperationException) { }
        catch (NullReferenceException) { }
    }


    void Update()
    {
        if (_cycle == null) return;

        if (_renderTasks.Count > 0)
        {
            _renderTasks.RemoveAll(t => t.IsCompleted);
        }


        if (Input.GetKeyDown(KeyCode.I))
        {
            var nodesInFOV = GetNodesInFOV();
            Debug.Log($"Nodes in view: {nodesInFOV.Count}/{_state.Nodes.Count}");


            var distinctRanges = new List<ByteRange>();

            foreach(var node in nodesInFOV)
            {
                var ranges = node.GetNodeRanges();
                distinctRanges.AddRange(ranges);
            }
            var normalized = ResourceDownloader.NormalizeRanges(distinctRanges).Distinct();

            Debug.Log($"Total size: {normalized.Sum(r => r.GetLength()) / 1_000_000L} MB");
        }

        if (Input.GetKeyDown(KeyCode.C))
            TrySaveCameraTransform();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //if (_settings.MetricsMode)
            //    Debug.Log(_vmafUtility.ExtractCSV());
            SceneManager.LoadSceneAsync(0);
        }

        //if (!_cycle.IsBusy() && _isMoving && BudgetStrategy == BudgetStrategy.Distance)
        //    await _cycle.Execute();
        ReportProgress();
    }

    bool IsNodeFullyOutOfView(Bounds bounds)
    {
        Vector3[] vertices = new Vector3[8];
        vertices[0] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z); // Front top right corner
        vertices[1] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z); // Front top left corner
        vertices[2] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z); // Front bottom right corner
        vertices[3] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z); // Front bottom left corner
        vertices[4] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z); // Back top right corner
        vertices[5] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z); // Back top left corner
        vertices[6] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z); // Back bottom right corner
        vertices[7] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z); // Back bottom left corner

        foreach (var vertex in vertices)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(vertex);
            if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1)
            {
                return false;
            }
        }

        return true;
    }


    private List<ExtendedNode> GetNodesInFOV()
    {
        var nodes = new List<ExtendedNode>();

        foreach(var node in _state.Nodes)
        {
            var boundingBox = _state.NodeBoundingBoxes[node];
            if (IsNodeFullyOutOfView(boundingBox.BoundingBox)) continue;
            nodes.Add(node);
        }
        return nodes;
    }



    private void ReportProgress()
    {
        if (!_hasRenderedAll && _cycle.HasRenderedAllNodes())
        {
            _hasRenderedAll = true;
            _stopwatch.Stop();
            Debug.Log($"Rendered all nodes in {_stopwatch.ElapsedMilliseconds}ms!!");
        }
        else
        {
            //var sb = new StringBuilder("Nodes to still render:\n");
            //foreach(var kvp in _state.NodeProgress)
            //{
            //    if (kvp.Value.RenderStatus != RenderStatus.Fully)
            //        sb.Append(kvp.Key.Name.ToString()); 
            //}
            //Debug.Log(sb.ToString());
        }
    }


    protected virtual bool IsFileLocked(FileInfo file)
    {
        try
        {
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }
}
