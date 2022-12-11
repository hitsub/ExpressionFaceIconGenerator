using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hitsub.ExpressionFaceIconGenerator.Scripts.Editor
{
    public class AnimationViewerGenerator
    {
        private GameObject m_animatedRoot;
        private Camera m_camera;

        public void Begin(GameObject animatedRoot)
        {
            m_animatedRoot = animatedRoot;

            m_camera = new GameObject().AddComponent<Camera>();

            var sceneCamera = SceneView.lastActiveSceneView.camera;
            m_camera.transform.position = sceneCamera.transform.position;
            m_camera.transform.rotation = sceneCamera.transform.rotation;
            var whRatio = (1f * sceneCamera.pixelWidth / sceneCamera.pixelHeight);
            m_camera.fieldOfView = whRatio < 1 ? sceneCamera.fieldOfView * whRatio : sceneCamera.fieldOfView;
            m_camera.orthographic = sceneCamera.orthographic;
            m_camera.nearClipPlane = sceneCamera.nearClipPlane;
            m_camera.farClipPlane = sceneCamera.farClipPlane;
            m_camera.orthographicSize = sceneCamera.orthographicSize;
            m_camera.clearFlags = CameraClearFlags.SolidColor;
            m_camera.backgroundColor = Color.gray;

        }

        public void ParentCameraTo(Transform newParent)
        {
            m_camera.transform.parent = newParent;
        }

        public void Terminate()
        {
            Object.DestroyImmediate(m_camera.gameObject);
        }

        public void Render(AnimationClip clip, Texture2D element, float normalizedTime)
        {
            var initPos = m_animatedRoot.transform.position;
            var initRot = m_animatedRoot.transform.rotation;
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(m_animatedRoot.gameObject, clip, normalizedTime * clip.length);
                AnimationMode.EndSampling();
                // This is a workaround for an issue where for some reason, the animator moves to the origin
                // after sampling despite the animation having no RootT/RootQ properties.
                m_animatedRoot.transform.position = initPos;
                m_animatedRoot.transform.rotation = initRot;

                var renderTexture = RenderTexture.GetTemporary(element.width, element.height, 24);
                renderTexture.wrapMode = TextureWrapMode.Clamp;

                RenderCamera(renderTexture, m_camera);
                RenderTextureTo(renderTexture, element);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                m_animatedRoot.transform.position = initPos;
                m_animatedRoot.transform.rotation = initRot;
            }
        }

        private static void RenderCamera(RenderTexture renderTexture, Camera camera)
        {
            var originalRenderTexture = camera.targetTexture;
            var originalAspect = camera.aspect;
            try
            {
                camera.targetTexture = renderTexture;
                camera.aspect = (float) renderTexture.width / renderTexture.height;
                camera.Render();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                camera.targetTexture = originalRenderTexture;
                camera.aspect = originalAspect;
            }
        }

        private static void RenderTextureTo(RenderTexture renderTexture, Texture2D texture2D)
        {
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
        }
    }
}