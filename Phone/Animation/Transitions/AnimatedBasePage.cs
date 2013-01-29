﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AnimatedBasePage.cs" company="XamlNinja">
//   2011 Richard Griffin and Ollie Riches
// </copyright>
// <summary>
// animated base page
// </summary>
// <credits>
// Kevin Marshall http://blogs.claritycon.com/blog/2010/10/13/wp7-page-transitions-sample/
// </credits>
// --------------------------------------------------------------------------------------------------------------------

using MyToolkit.Paging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace MyToolkit.Animation.Transitions
{
	/// <summary>
    /// Animated base page
    /// </summary>
    public class AnimatedBasePage : PhoneApplicationPage
    {
        #region Constants and Fields

        /// <summary>
        /// AnimationContextProperty
        /// </summary>
        public static readonly DependencyProperty AnimationContextProperty =
            DependencyProperty.Register("AnimationContext", typeof(FrameworkElement), typeof(AnimatedBasePage), new PropertyMetadata(null));

        /// <summary>
        /// The external uri.
        /// </summary>
        private static readonly Uri ExternalUri = new Uri(@"app://external/");

        /// <summary>
        /// The from uri.
        /// </summary>
        private static Uri fromUri;

        /// <summary>
        /// The is navigating.
        /// </summary>
        private static bool isNavigating;

        /// <summary>
        /// The arrived from uri.
        /// </summary>
        private Uri arrivedFromUri;

        /// <summary>
        /// The current animation type.
        /// </summary>
        private AnimationType currentAnimationType;

        /// <summary>
        /// The current navigation mode.
        /// </summary>
        private NavigationMode? currentNavigationMode;

        /// <summary>
        /// The is active.
        /// </summary>
        private bool isActive;

        /// <summary>
        /// The is animating.
        /// </summary>
        private bool isAnimating;

        /// <summary>
        /// The is forward navigation.
        /// </summary>
        private bool isForwardNavigation;

        /// <summary>
        /// The loading and animating in.
        /// </summary>
        private bool loadingAndAnimatingIn;

        /// <summary>
        /// The needs outro animation.
        /// </summary>
        private bool needsOutroAnimation;

        /// <summary>
        /// The next uri.
        /// </summary>
        private Uri nextUri;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimatedBasePage"/> class.
        /// </summary>
        public AnimatedBasePage()
        {
            this.isActive = true;
            this.isForwardNavigation = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the animation context.
        /// </summary>
        /// <value>The animation context.</value>
        public FrameworkElement AnimationContext
        {
            get
            {
                return (FrameworkElement)this.GetValue(AnimationContextProperty);
            }

            set
            {
                this.SetValue(AnimationContextProperty, value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cancels the animation.
        /// </summary>
        public void CancelAnimation()
        {
            isActive = false;
        }
		
        /// <summary>
        /// Resumes the animation.
        /// </summary>
        public void ResumeAnimation()
        {
            isActive = true;
        }

        #endregion

        #region Methods

        protected virtual void AnimationsComplete(AnimationType animationType)
        {

        }

        /// <summary>
        /// Gets the animation.
        /// </summary>
        /// <param name="animationType">
        /// Type of the animation.
        /// </param>
        /// <param name="toOrFrom">
        /// To or from.
        /// </param>
        /// <returns>
        /// </returns>
        protected virtual AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            AnimatorHelperBase animation;

            switch (animationType)
            {
                case AnimationType.NavigateBackwardIn:
                    animation = new TurnstileBackwardInAnimator();
                    break;

                case AnimationType.NavigateBackwardOut:
                    animation = new TurnstileBackwardOutAnimator();
                    break;

                case AnimationType.NavigateForwardIn:
                    animation = new TurnstileForwardInAnimator();
                    break;

                default:
                    animation = new TurnstileForwardOutAnimator();
                    break;
            }

            animation.RootElement = this.AnimationContext;
            return animation;
        }

        /// <summary>
        /// Determines whether [is popup open].
        /// </summary>
        /// <returns>
        /// <c>true</c> if [is popup open]; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsPopupOpen()
        {
            return false;
        }

        /// <summary>
        /// This method is called when the hardware back key is pressed.
        /// </summary>
        /// <param name="e">
        /// Set e.Cancel to true to indicate that the request was handled by the application.
        /// </param>
        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (isNavigating)
            {
                e.Cancel = true;
                return;
            }

            if (!CanAnimate())
            {
                return;
            }

            if (isAnimating)
            {
                e.Cancel = true;
                return;
            }

            if (loadingAndAnimatingIn)
            {
                e.Cancel = true;
                return;
            }

            if (!NavigationService.CanGoBack)
				return;
            if (IsPopupOpen())
                return;

            isNavigating = true;
            e.Cancel = true;
            
			needsOutroAnimation = false;
            currentAnimationType = AnimationType.NavigateBackwardOut;
            currentNavigationMode = NavigationMode.Back;

            RunAnimation();
        }

        /// <summary>
        /// Called when [first layout updated].
        /// </summary>
        /// <param name="isBackNavigation">
        /// if set to <c>true</c> [is back navigation].
        /// </param>
        /// <param name="from">
        /// From.
        /// </param>
        protected virtual void OnFirstLayoutUpdated(bool isBackNavigation, Uri from)
        {
        }

        /// <summary>
        /// Called when a page becomes the active page in a frame.
        /// </summary>
        /// <param name="e">
        /// An object that contains the event data.
        /// </param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
			currentNavigationMode = null;
			
			if (nextUri != ExternalUri)
			{
				loadingAndAnimatingIn = true;
				Loaded += AnimatedBasePageLoaded;
				if (AnimationContext != null)
					AnimationContext.Opacity = 0;
			}
            
			needsOutroAnimation = true;
        }

        /// <summary>
        /// Called just before a page is no longer the active page in a frame.
        /// </summary>
        /// <param name="e">
        /// An object that contains the event data.
        /// </param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (this.isAnimating)
            {
                e.Cancel = true;
                return;
            }

            if (this.loadingAndAnimatingIn)
            {
                e.Cancel = true;
                return;
            }

            fromUri = this.NavigationService.CurrentSource;

            if (!this.needsOutroAnimation)
            {
                return;
            }

            this.needsOutroAnimation = false;

            if (!this.CanAnimate())
            {
                return;
            }

            if (isNavigating)
            {
                e.Cancel = true;
                return;
            }

            if (!this.NavigationService.CanGoBack && e.NavigationMode == NavigationMode.Back)
            {
                return;
            }

            if (this.IsPopupOpen())
            {
                return;
            }

            e.Cancel = true;
            this.nextUri = e.Uri;

            switch (e.NavigationMode)
            {
                case NavigationMode.New:
                    this.currentAnimationType = AnimationType.NavigateForwardOut;
                    break;

                case NavigationMode.Back:
                    this.currentAnimationType = AnimationType.NavigateBackwardOut;
                    break;

                case NavigationMode.Forward:
                    this.currentAnimationType = AnimationType.NavigateForwardOut;
                    break;
            }

            this.currentNavigationMode = e.NavigationMode;

            if (e.Uri != ExternalUri)
            {
                this.RunAnimation();
            }
        }

        /// <summary>
        /// Animateds the base page loaded.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void AnimatedBasePageLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.AnimatedBasePageLoaded;
            this.OnLayoutUpdated(this, null);
        }

        /// <summary>
        /// Determines whether this instance can animate.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instance can animate; otherwise, <c>false</c>.
        /// </returns>
        private bool CanAnimate()
        {
            return this.isActive && !isNavigating && this.AnimationContext != null;
        }

        /// <summary>
        /// Called when [layout updated].
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="System.EventArgs"/> instance containing the event data.
        /// </param>
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            if (this.isForwardNavigation)
            {
                this.currentAnimationType = AnimationType.NavigateForwardIn;
                this.arrivedFromUri = fromUri != null ? new Uri(fromUri.OriginalString, UriKind.Relative) : null;
            }
            else
            {
                this.currentAnimationType = AnimationType.NavigateBackwardIn;
            }

            if (this.CanAnimate())
            {
                this.RunAnimation();
            }
            else
            {
                if (this.AnimationContext != null)
                {
                    this.AnimationContext.Opacity = 1;
                }

                this.OnTransitionAnimationCompleted();
            }

            // OnFirstLayoutUpdated(!_isForwardNavigation, _fromUri);
            if (this.isForwardNavigation)
            {
                this.isForwardNavigation = false;
            }
        }

        /// <summary>
        /// Called when [transition animation completed].
        /// </summary>
        private void OnTransitionAnimationCompleted()
        {
            isAnimating = false;
            loadingAndAnimatingIn = false;
			try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    switch (currentNavigationMode)
                    {
                        case NavigationMode.Forward:
		                    PhonePage.CurrentPage.NavigationService.GoForward();
                            break;

                        case NavigationMode.Back:
		                    PhonePage.CurrentPage.NavigationService.GoBack();
                            break;

                        case NavigationMode.New:
		                    PhonePage.CurrentPage.NavigationService.Navigate(nextUri);
                            break;
                    }
					isNavigating = false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OnTransitionAnimationCompleted Exception on {0}: {1}", this, ex);
            }
			AnimationsComplete(currentAnimationType);
        }

        /// <summary>
        /// Runs the animation.
        /// </summary>
        private void RunAnimation()
        {
            isAnimating = true;
			AnimatorHelperBase animation = null;

            switch (currentAnimationType)
            {
                case AnimationType.NavigateForwardIn:
                    animation = GetAnimation(currentAnimationType, fromUri);
                    break;
                case AnimationType.NavigateBackwardOut:
                    animation = GetAnimation(currentAnimationType, arrivedFromUri);
                    break;
                default:
                    animation = GetAnimation(currentAnimationType, nextUri);
                    break;
            }

			if (animation == null)
			{
				AnimationContext.Opacity = 1;
				OnTransitionAnimationCompleted();
			}
			else
			{
				Dispatcher.BeginInvoke(() =>
				{
					AnimationContext.Opacity = 1;
					animation.Begin(OnTransitionAnimationCompleted);
				});
			}
        }

        #endregion
    }
}