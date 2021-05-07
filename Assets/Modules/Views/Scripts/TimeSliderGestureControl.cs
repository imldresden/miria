// ------------------------------------------------------------------------------------
// <copyright file="TimeSliderGestureControl.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace IMLD.MixedRealityAnalysis.Views
{
    /// <summary>
    /// This Unity component updates the slider for the timeline.
    /// </summary>
    public class TimeSliderGestureControl : MonoBehaviour, IMixedRealityGestureHandler<Vector3>, IMixedRealityPointerHandler
    {
        [Tooltip("Sends slider event information on Update")]
        public UnityEvent OnUpdateEvent;

        public float Speed = 0.01f;

        private readonly float maxSliderValue = 1;

        private readonly float minSliderValue = 0;

        private float sliderValue = 0;

        /// <summary>
        /// Gets or sets the value of the slider.
        /// </summary>
        public float SliderValue
        {
            get
            {
                return sliderValue;
            }

            set
            {
                if (sliderValue != value)
                {
                    sliderValue = value;
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
            Debug.Log("OnGestureCanceled");
            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.
        }

        /// <summary>
        /// Gets raised when gesture input has been completed. Implements <see cref="IMixedRealityGestureHandler{Vector3}"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureCompleted(InputEventData<Vector3> eventData)
        {
            Debug.Log("OnGestureCompleted");
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
            eventData.Use();
        }

        /// <summary>
        /// Gets raised when gesture input has been updated. Implements <see cref="IMixedRealityGestureHandler{Vector3}"/>.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public void OnGestureUpdated(InputEventData<Vector3> eventData)
        {
            eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.

            Vector3 vectorValue = Quaternion.Inverse(CameraCache.Main.transform.rotation) * eventData.InputData;
            SliderValue = Mathf.Clamp(SliderValue + (vectorValue.x * Speed), minSliderValue, maxSliderValue);
            Debug.Log("Value: " + SliderValue + "(+ " + (vectorValue.x * Speed) + ")");
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
            if (target && target.name.Equals("Slider"))
            {
                bool didHit = Physics.Raycast(CameraCache.Main.transform.position, CameraCache.Main.transform.forward, out RaycastHit hitInfo, 4);
                if (didHit)
                {
                    Vector3 positionOnSlider = transform.InverseTransformPoint(hitInfo.point);
                    SliderValue = Mathf.Clamp(positionOnSlider.x + 0.5f, minSliderValue, maxSliderValue);
                }

                eventData.Use();
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
        /// Overrides the slider value.
        /// </summary>
        /// <param name="value">The new value, gets clamped so that it is not smaller than the minimum or larger than the maximum.</param>
        public void SetSliderValue(float value)
        {
            sliderValue = Mathf.Clamp(value, minSliderValue, maxSliderValue); // do not trigger update!
        }
    }
}