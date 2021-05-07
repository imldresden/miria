// ---------------------------------------------------------------------------------------------
// <copyright file="TimeFilterSliderGestureControl.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ---------------------------------------------------------------------------------------------

using System;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component updates the range slider for the <see cref="TimeFilter"/>.
    /// </summary>
    public class TimeFilterSliderGestureControl : MonoBehaviour, IMixedRealityGestureHandler<Vector3>, IMixedRealityPointerHandler
    {
        public GameObject LeftSlider;

        [Tooltip("Sends slider event information on Update")]
        public UnityEvent OnUpdateEvent;

        public GameObject RightSlider;

        public float Speed = 0.01f;

        private bool isManipulatingLeftSlider = false;

        private readonly float maxSliderValue = 1;

        private readonly float minSliderValue = 0;

        private float sliderValueMax = 1;

        private float sliderValueMin = 0;

        /// <summary>
        /// Gets the maximum value of the slider.
        /// </summary>
        public float SliderValueMax
        {
            get
            {
                return sliderValueMax;
            }

            private set
            {
                if (sliderValueMax != value)
                {
                    sliderValueMax = value;
                    OnUpdateEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets the minimum value of the slider.
        /// </summary>
        public float SliderValueMin
        {
            get
            {
                return sliderValueMin;
            }

            private set
            {
                if (sliderValueMin != value)
                {
                    sliderValueMin = value;
                    OnUpdateEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets raised when gesture input has been canceled. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureCanceled(InputEventData eventData)
        {
            Debug.Log("OnNavigationCanceled");
            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.
        }

        /// <summary>
        /// Gets raised when gesture input has been completed. Implements <see cref="IMixedRealityGestureHandler{Vector3}"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureCompleted(InputEventData<Vector3> eventData)
        {
            Debug.Log("OnNavigationCompleted");
            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.
        }

        /// <summary>
        /// Gets raised when gesture input has been completed. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureCompleted(InputEventData eventData)
        {
        }

        /// <summary>
        /// Gets raised when gesture input has been started. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureStarted(InputEventData eventData)
        {
            if (eventData.selectedObject == LeftSlider)
            {
                isManipulatingLeftSlider = true;
            }
            else if (eventData.selectedObject == RightSlider)
            {
                isManipulatingLeftSlider = false;
            }
        }

        /// <summary>
        /// Gets raised when gesture input has been updated. Implements <see cref="IMixedRealityGestureHandler{Vector3}"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureUpdated(InputEventData<Vector3> eventData)
        {
            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.

            Vector3 vectorValue = Quaternion.Inverse(CameraCache.Main.transform.rotation) * eventData.InputData;

            Debug.Log("OnGesturenUpdated: " + vectorValue.x);

            if (isManipulatingLeftSlider)
            {
                SliderValueMin = Mathf.Clamp(SliderValueMin + (vectorValue.x * Speed), minSliderValue, sliderValueMax);
            }
            else
            {
                SliderValueMax = Mathf.Clamp(SliderValueMax + (vectorValue.x * Speed), sliderValueMin, maxSliderValue);
            }
        }

        /// <summary>
        /// Gets raised when gesture input has been updated. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureUpdated(InputEventData eventData)
        {
        }

        /// <summary>
        /// Gets raised when clicked. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The click event data.</param>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            GameObject target = eventData.Pointer.Result.CurrentPointerTarget;
            if (target && target.name.Equals("FilterSlider"))
            {
                bool didHit = Physics.Raycast(CameraCache.Main.transform.position, CameraCache.Main.transform.forward, out RaycastHit hitInfo, 4);
                if (didHit)
                {
                    Vector3 positionOnSlider = transform.InverseTransformPoint(hitInfo.point);
                    float newSliderPosition = Mathf.Clamp(positionOnSlider.x + 0.5f, minSliderValue, maxSliderValue);
                    if (Math.Abs(newSliderPosition - SliderValueMin) < Math.Abs(newSliderPosition - SliderValueMax))
                    {
                        // closer to left slider
                        SliderValueMin = newSliderPosition;
                    }
                    else
                    {
                        // closer to right slider
                        SliderValueMax = newSliderPosition;
                    }
                }
            }
        }

        /// <summary>
        /// Gets raised when pointer is down. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The pointer down event data.</param>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Gets raised when pointer is dragged. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The pointer dragged event data.</param>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Gets raised when pointer is up. Implements <see cref="IMixedRealityPointerHandler"/>.
        /// </summary>
        /// <param name="eventData">The pointer up event data.</param>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
        }

        /// <summary>
        /// Overrides the right slider value.
        /// </summary>
        /// <param name="value">The new value, gets clamped so that it is not smaller than the minimum.</param>
        public void SetMaxSliderValue(float value)
        {
            sliderValueMax = Mathf.Clamp(value, sliderValueMin, maxSliderValue); // do not trigger update!
        }

        /// <summary>
        /// Overrides the left slider value.
        /// </summary>
        /// <param name="value">The new value, gets clamped so that it is not larger than the maximum.</param>
        public void SetMinSliderValue(float value)
        {
            sliderValueMin = Mathf.Clamp(value, minSliderValue, sliderValueMax); // do not trigger update!
        }
    }
}