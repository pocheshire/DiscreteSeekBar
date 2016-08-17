# DiscreteSeekBar

## What?
[DiscreteSeekBar](https://github.com/AnderWeb/discreteSeekBar) ported to C#

A custom view component that mimics the [Material Design Discrete slider pattern](https://material.google.com/components/sliders.html#sliders-discrete-slider).

##NuGet package

Coming soon...

##Dependencies
It uses **com.android.support:support-v4** as the only dependency.

## How?

Put them into your layouts like:
```xml
<dsb.DiscreteSeekBar
    android:id="@+id/seekbar1"
    android:layout_width="match_parent"
    android:layout_height="wrap_content"
    app:dsb_min="100"
    app:dsb_max="10000"
    app:dsb_value="100" />
```

####Parameters
You can tweak a few things of the DiscreteSeekbar:

* **dsb_min**: minimum value
* **dsb_max**: maximum value
* **dsb_value**: current value
* **dsb_mirrorForRtl**: reverse the DiscreteSeekBar for RTL locales
* **dsb_allowTrackClickToDrag**: allows clicking outside the thumb circle to initiate drag. Default TRUE
* **dsb_indicatorFormatter**: a string [Format] to apply to the value inside the bubble indicator.
* **dsb_indicatorPopupEnabled**: choose if the bubble indicator will be shown. Default TRUE 

####Design
 
* **dsb_progressColor**: color/colorStateList for the progress bar and thumb drawable
* **dsb_trackColor**: color/colorStateList for the track drawable
* **dsb_indicatorTextAppearance**: TextAppearance for the bubble indicator
* **dsb_indicatorColor**: color/colorStateList for the bubble shaped drawable
* **dsb_indicatorElevation**: related to android:elevation. Will only be used on API level 21+
* **dsb_rippleColor**: color/colorStateList for the ripple drawable seen when pressing the thumb. (Yes, it does a kind of "ripple" on API levels lower than 21 and a real RippleDrawable for 21+.
* **dsb_trackHeight**: dimension for the height of the track drawable.
* **dsb_scrubberHeight**: dimension for the height of the scrubber (selected area) drawable.
* **dsb_thumbSize**: dimension for the size of the thumb drawable.
* **dsb_indicatorSeparation**: dimension for the vertical distance from the thumb to the indicator. 

You can also use the attribute **discreteSeekBarStyle** on your themes with a custom Style to be applied to all the DiscreteSeekBars on your app/activity/fragment/whatever.

##License
```
DiscreteSeekBar library for Android ported to C#
Copyright 2014 Gustavo Claramunt (Ander Webbs)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```