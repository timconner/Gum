﻿using SkiaGum.Renderables;
using SkiaSharp.Skottie;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.GueDeriving
{
    public class LottieAnimationRuntime : BindableGraphicalUiElement
    {
        //protected override RenderableBase ContainedRenderable => ContainedLottieAnimation;

        LottieAnimation mContainedLottieAnimation;
        LottieAnimation ContainedLottieAnimation
        {
            get
            {
                if (mContainedLottieAnimation == null)
                {
                    mContainedLottieAnimation = this.RenderableComponent as LottieAnimation;
                }
                return mContainedLottieAnimation;
            }
        }

        string sourceFile;
        public string SourceFile
        {
            // eventually we may want to store this off somehow
            get => sourceFile;
            set
            {
                if (sourceFile != value)
                {
                    sourceFile = value;
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    var contentLoader = loaderManager.ContentLoader;
                    var animation = contentLoader.LoadContent<Animation>(value);
                    Animation = animation;
                }
            }
        }

        public Animation Animation
        {
            get => ContainedLottieAnimation.Animation;
            set => ContainedLottieAnimation.Animation = value;
        }

        //public bool IsDimmed
        //{
        //    get => ContainedCircle.IsDimmed;
        //    set => ContainedCircle.IsDimmed = value;
        //}

        public LottieAnimationRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                SetContainedObject(new LottieAnimation());
                //this.Color = SKColors.White;
                Width = 100;
                Height = 100;
            }
        }
    }
}
