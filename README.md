# AnimationHelper
This small script for Unity will help you with keyframes copying.  
Default unity copying won't paste keyframes if there is no parameteres for them in animation. This script will automatically add all necessary parameters to animation you copy to (excluding parameters that cannot be added because they are not presented in any object within hierarchy of object with animator component).  
You can drop this script in any folder within "Assets" folder.  
To copy or paste you can use shortcuts "Alt + C" and "Alt + V", or choose action in menu tab named "AnimationHelper".  
This cript were tested on Unity 2020.1.2f1. There may be bugs in other versions of Unity because script using reflection to acces animation window.
