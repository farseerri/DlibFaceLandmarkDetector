﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using DlibFaceLandmarkDetector;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// Face tracker AR from WebCamTexture Example.
    /// This example was referring to http://www.morethantechnical.com/2012/10/17/head-pose-estimation-with-opencv-opengl-revisited-w-code/
    /// and use effect asset from http://ktk-kumamoto.hatenablog.com/entry/2014/09/14/092400
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureARExample : MonoBehaviour
    {
        /// <summary>
        /// The is showing face points.
        /// </summary>
        public bool isShowingFacePoints;
        
        /// <summary>
        /// The is showing face points toggle.
        /// </summary>
        public Toggle isShowingFacePointsToggle;
        
        /// <summary>
        /// The is showing axes.
        /// </summary>
        public bool isShowingAxes;
        
        /// <summary>
        /// The is showing axes toggle.
        /// </summary>
        public Toggle isShowingAxesToggle;
        
        /// <summary>
        /// The is showing head.
        /// </summary>
        public bool isShowingHead;
        
        /// <summary>
        /// The is showing head toggle.
        /// </summary>
        public Toggle isShowingHeadToggle;
        
        /// <summary>
        /// The is showing effects.
        /// </summary>
        public bool isShowingEffects;
        
        /// <summary>
        /// The is showing effects toggle.
        /// </summary>
        public Toggle isShowingEffectsToggle;
        
        /// <summary>
        /// The axes.
        /// </summary>
        public GameObject axes;
        
        /// <summary>
        /// The head.
        /// </summary>
        public GameObject head;
        
        /// <summary>
        /// The right eye.
        /// </summary>
        public GameObject rightEye;
        
        /// <summary>
        /// The left eye.
        /// </summary>
        public GameObject leftEye;
        
        /// <summary>
        /// The mouth.
        /// </summary>
        public GameObject mouth;
        
        /// <summary>
        /// The mouth particle system.
        /// </summary>
        ParticleSystem[] mouthParticleSystem;
        
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;
        
        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera ARCamera;
        
        /// <summary>
        /// The cam matrix.
        /// </summary>
        Mat camMatrix;
        
        /// <summary>
        /// The dist coeffs.
        /// </summary>
        MatOfDouble distCoeffs;
        
        /// <summary>
        /// The transformation m.
        /// </summary>
        Matrix4x4 transformationM = new Matrix4x4 ();
        
        /// <summary>
        /// The invert Z.
        /// </summary>
        Matrix4x4 invertZM;
        
        /// <summary>
        /// The ar game object.
        /// </summary>
        public GameObject ARGameObject;
        
        /// <summary>
        /// The 3d face object points.
        /// </summary>
        MatOfPoint3f objectPoints;
        
        /// <summary>
        /// The image points.
        /// </summary>
        MatOfPoint2f imagePoints;
        
        /// <summary>
        /// The rvec.
        /// </summary>
        Mat rvec;
        
        /// <summary>
        /// The tvec.
        /// </summary>
        Mat tvec;
        
        /// <summary>
        /// The rot mat.
        /// </summary>
        Mat rotMat;
        
        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        
        /// <summary>
        /// The shape_predictor_68_face_landmarks_dat_filepath.
        /// </summary>
        private string shape_predictor_68_face_landmarks_dat_filepath;
        
        // Use this for initialization
        void Start ()
        {
            isShowingFacePointsToggle.isOn = isShowingFacePoints;
            isShowingAxesToggle.isOn = isShowingAxes;
            isShowingHeadToggle.isOn = isShowingHead;
            isShowingEffectsToggle.isOn = isShowingEffects;
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(DlibFaceLandmarkDetector.Utils.getFilePathAsync("shape_predictor_68_face_landmarks.dat", (result) => {
                shape_predictor_68_face_landmarks_dat_filepath = result;
                Run ();
            }));
            #else
            shape_predictor_68_face_landmarks_dat_filepath = DlibFaceLandmarkDetector.Utils.getFilePath ("shape_predictor_68_face_landmarks.dat");
            Run ();
            #endif
        }
        
        private void Run ()
        {
            //set 3d face object points.
            objectPoints = new MatOfPoint3f (
                new Point3 (-31, 72, 86),//l eye
                new Point3 (31, 72, 86),//r eye
                new Point3 (0, 40, 114),//nose
                new Point3 (-20, 15, 90),//l mouse
                new Point3 (20, 15, 90),//r mouse
                new Point3 (-69, 76, -2),//l ear
                new Point3 (69, 76, -2)//r ear
                );
            imagePoints = new MatOfPoint2f ();
            rvec = new Mat ();
            tvec = new Mat ();
            rotMat = new Mat (3, 3, CvType.CV_64FC1);
            
            faceLandmarkDetector = new FaceLandmarkDetector (shape_predictor_68_face_landmarks_dat_filepath);
            
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Init ();
        }
        
        /// <summary>
        /// Raises the web cam texture to mat helper inited event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInited ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInited");
            
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();
            
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);
            
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            
            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
            
            
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
            
            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
            
            
            //set cameraparam
            int max_d = (int)Mathf.Max (width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;
            camMatrix = new Mat (3, 3, CvType.CV_64FC1);
            camMatrix.put (0, 0, fx);
            camMatrix.put (0, 1, 0);
            camMatrix.put (0, 2, cx);
            camMatrix.put (1, 0, 0);
            camMatrix.put (1, 1, fy);
            camMatrix.put (1, 2, cy);
            camMatrix.put (2, 0, 0);
            camMatrix.put (2, 1, 0);
            camMatrix.put (2, 2, 1.0f);
            Debug.Log ("camMatrix " + camMatrix.dump ());
            
            
            distCoeffs = new MatOfDouble (0, 0, 0, 0);
            Debug.Log ("distCoeffs " + distCoeffs.dump ());
            
            
            //calibration camera
            Size imageSize = new Size (width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point (0, 0);
            double[] aspectratio = new double[1];
            
            Calib3d.calibrationMatrixValues (camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);
            
            Debug.Log ("imageSize " + imageSize.ToString ());
            Debug.Log ("apertureWidth " + apertureWidth);
            Debug.Log ("apertureHeight " + apertureHeight);
            Debug.Log ("fovx " + fovx [0]);
            Debug.Log ("fovy " + fovy [0]);
            Debug.Log ("focalLength " + focalLength [0]);
            Debug.Log ("principalPoint " + principalPoint.ToString ());
            Debug.Log ("aspectratio " + aspectratio [0]);
            
            
            //To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan ((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2 ((float)cx, (float)fx) + Mathf.Atan2 ((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan ((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2 ((float)cy, (float)fy) + Mathf.Atan2 ((float)(imageSize.height - cy), (float)fy));
            
            Debug.Log ("fovXScale " + fovXScale);
            Debug.Log ("fovYScale " + fovYScale);
            
            
            //Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale) {
                ARCamera.fieldOfView = (float)(fovx [0] * fovXScale);
            } else {
                ARCamera.fieldOfView = (float)(fovy [0] * fovYScale);
            }
            
            
            invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
            Debug.Log ("invertZM " + invertZM.ToString ());
            
            
            axes.SetActive (false);
            head.SetActive (false);
            rightEye.SetActive (false);
            leftEye.SetActive (false);
            mouth.SetActive (false);
            
            mouthParticleSystem = mouth.GetComponentsInChildren<ParticleSystem> (true);
        }
        
        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");
            
            camMatrix.Dispose ();
            distCoeffs.Dispose ();
        }
        
        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode){
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }
        
        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
                
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();
                
                
                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
                
                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect ();
                
                if (detectResult.Count > 0) {
                    
                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (detectResult [0]);
                    
                    if (isShowingFacePoints)
                        OpenCVForUnityUtils.DrawFaceLandmark (rgbaMat, points, new Scalar (0, 255, 0, 255), 2);
                    
                    imagePoints.fromArray (
                        new Point ((points [38].x + points [41].x) / 2, (points [38].y + points [41].y) / 2),//l eye
                        new Point ((points [43].x + points [46].x) / 2, (points [43].y + points [46].y) / 2),//r eye
                        new Point (points [33].x, points [33].y),//nose
                        new Point (points [48].x, points [48].y),//l mouth
                        new Point (points [54].x, points [54].y) //r mouth
                        ,
                        new Point (points [0].x, points [0].y),//l ear
                        new Point (points [16].x, points [16].y)//r ear
                        );
                    
                    
                    Calib3d.solvePnP (objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);
                    
                    
                    if (tvec.get (2, 0) [0] > 0) {
                        
                        if (Mathf.Abs ((float)(points [43].y - points [46].y)) > Mathf.Abs ((float)(points [42].x - points [45].x)) / 6.0) {
                            if (isShowingEffects)
                                rightEye.SetActive (true);
                        }
                        
                        if (Mathf.Abs ((float)(points [38].y - points [41].y)) > Mathf.Abs ((float)(points [39].x - points [36].x)) / 6.0) {
                            if (isShowingEffects)
                                leftEye.SetActive (true);
                        }
                        if (isShowingHead)
                            head.SetActive (true);
                        if (isShowingAxes)
                            axes.SetActive (true);
                        
                        
                        float noseDistance = Mathf.Abs ((float)(points [27].y - points [33].y));
                        float mouseDistance = Mathf.Abs ((float)(points [62].y - points [66].y));
                        if (mouseDistance > noseDistance / 5.0) {
                            if (isShowingEffects) {
                                mouth.SetActive (true);
                                foreach (ParticleSystem ps in mouthParticleSystem) {
                                    ps.enableEmission = true;
                                    ps.startSize = 40 * (mouseDistance / noseDistance);
                                }
                            }
                        } else {
                            if (isShowingEffects) {
                                foreach (ParticleSystem ps in mouthParticleSystem) {
                                    ps.enableEmission = false;
                                }
                            }
                        }
                        
                        
                        // position
                        Vector4 pos =  new Vector4((float)tvec.get (0, 0) [0], (float)tvec.get (1, 0) [0], (float)tvec.get (2, 0) [0], 0); // from OpenCV
                        // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                        ARGameObject.transform.localPosition = new Vector3(pos.x, -pos.y, pos.z);
                        
                        
                        // rotation
                        Calib3d.Rodrigues (rvec, rotMat);
                        
                        Vector3 forward = new Vector3((float)rotMat.get (0, 2) [0],(float)rotMat.get (1, 2) [0],(float)rotMat.get (2, 2) [0]); // from OpenCV
                        Vector3 up = new Vector3((float)rotMat.get (0, 1) [0],(float)rotMat.get (1, 1) [0],(float)rotMat.get (2, 1) [0]); // from OpenCV
                        // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                        Quaternion rot = Quaternion.LookRotation(new Vector3(forward.x, -forward.y, forward.z), new Vector3(up.x, -up.y, up.z));
                        ARGameObject.transform.localRotation = rot;
                        
                        
                        // Apply Z axis inverted matrix.
                        invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
                        transformationM = ARGameObject.transform.localToWorldMatrix * invertZM;
                        
                        // Apply camera transform matrix.
                        transformationM = ARCamera.transform.localToWorldMatrix * transformationM ;
                        
                        ARUtils.SetTransformFromMatrix (ARGameObject.transform, ref transformationM);
                    }
                }
                
                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
                
                OpenCVForUnity.Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }
        
        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose ();
            
            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();
        }
        
        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DlibFaceLandmarkDetectorExample");
            #else
            Application.LoadLevel ("DlibFaceLandmarkDetectorExample");
            #endif
        }
        
        /// <summary>
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton ()
        {
            webCamTextureToMatHelper.Play ();
        }
        
        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton ()
        {
            webCamTextureToMatHelper.Pause ();
        }
        
        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton ()
        {
            webCamTextureToMatHelper.Stop ();
        }
        
        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton ()
        {
            webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
        }
        
        /// <summary>
        /// Raises the is showing face points toggle event.
        /// </summary>
        public void OnIsShowingFacePointsToggle ()
        {
            if (isShowingFacePointsToggle.isOn) {
                isShowingFacePoints = true;
            } else {
                isShowingFacePoints = false;
            }
        }
        
        /// <summary>
        /// Raises the is showing axes toggle event.
        /// </summary>
        public void OnIsShowingAxesToggle ()
        {
            if (isShowingAxesToggle.isOn) {
                isShowingAxes = true;
            } else {
                isShowingAxes = false;
                axes.SetActive (false);
            }
        }
        
        /// <summary>
        /// Raises the is showing head toggle event.
        /// </summary>
        public void OnIsShowingHeadToggle ()
        {
            if (isShowingHeadToggle.isOn) {
                isShowingHead = true;
            } else {
                isShowingHead = false;
                head.SetActive (false);
            }
        }
        
        /// <summary>
        /// Raises the is showin effects toggle event.
        /// </summary>
        public void OnIsShowinEffectsToggle ()
        {
            if (isShowingEffectsToggle.isOn) {
                isShowingEffects = true;
            } else {
                isShowingEffects = false;
                rightEye.SetActive (false);
                leftEye.SetActive (false);
                mouth.SetActive (false);
            }
        }
    }
}