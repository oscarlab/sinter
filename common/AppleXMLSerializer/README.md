# README #

How to maintain RoleMappings.plist file?

### entity_class dictionary ###

* Keys are the name of the model classes, i.e. class Entity, class Header. It is very important to keep in mind that the Class name and case must identical to the definition of actual classes.

* Class name must follow camelCase convention. 

* Values are an Array representing the attributes of a model class. For example, Screen class has two attributes -- screen_height, screen_height.

* Each attribute can either be (i) an instance of another model class (such as attribute 'header' in Class 'Sinter' is an instance of class 'Header'), or (ii) an array, such as attribute 'children' in class 'Entity'.

* In case of (i) the naming convention of attributes should be 'underscore' separated name of their corresponding Class name. For example, class name 'MouseOrCaret' is in camelCase and its instance should 'mouse_or_caret'.