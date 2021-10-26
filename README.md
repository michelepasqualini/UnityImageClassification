# Unity Image Classification based on Barracuda

This is an example of using models trained with TensorFlow or ONNX in Unity application for image classification and object detection. 
It uses Barracuda inference engine - please note that Barracuda is still in development preview and changes frequently.

# How

- Open the project in Unity.

- Install Barracuda 2.1.0-preview plugin from ```Window -> Package Manager```

- Open Classify (named Detector) scene in Assets folder.

- Make sure that Classifier object has Model file and Labels file set.

- in ```File -> Build``` settings choose one of the scenes and hit Build and run.

Barracuda repository might be found [here](https://github.com/Unity-Technologies/barracuda-release)
