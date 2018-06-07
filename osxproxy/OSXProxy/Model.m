//
//  RemoteControl.m
//  Hello
//
//  Created by Syed Masum Billah on 7/12/14.
//
//

#import "Model.h"
#import "ControlTypes.h"

@implementation Model

- (id) init {
    if ( self = [super init] ) {
        _isDuplicate = NO;
    }
    return self;
}



-(id) initWithEntity:(Entity*) entity{
    if ( self = [super init] ) {
        _isDuplicate = NO;
        _name       = entity.name;
        _process_id = entity.process_id;
        _unique_id = entity.unique_id;
    }
    return self;
}


- (BOOL) isEqualToUI:(id) object{
    Model* other = object;
    if (!object) {
        return NO;
    }
    if(![object isKindOfClass:[Model class]]){
        return NO;
    }
    
    if (_top != other.top || _left != other.left || _width!= other.width || _height !=other.height || _child_count !=  other.child_count || ![_name isEqualToString:other.name] || ![_value isEqualToString:other.value] || ![_process_id isEqualToString:other.process_id]) {
        return NO;
    }
    return YES;
}

// copy method
- (id) copyWithZone:(NSZone *)zone {
    Model *modelCopy = [[Model allocWithZone: zone] init];
    modelCopy.isDuplicate = YES;
    modelCopy.type = [_type copy];
    modelCopy.name = [_name copy];
    modelCopy.value = [_value copy];
    modelCopy.process_id = [_process_id copy];
    modelCopy.unique_id = [_unique_id copy];
    
    modelCopy.child_count = _child_count;
    modelCopy.states = _states;
    modelCopy.left = _left;
    modelCopy.top = _top;
    modelCopy.height = _height;
    modelCopy.width = _width;
    // note: parent is not copied
    modelCopy.parent = nil;
    
    if (modelCopy.child_count) {
        modelCopy.children = [[NSMutableArray alloc] init];
        for(Model* m in _children){
            [[modelCopy children] addObject:[m copy]];
        }
        // add parent
        for(Model* m in [modelCopy children]){
            m.parent = modelCopy;
        }
    }
    return modelCopy;
}

- (NSString*) toString{
    int true_count = _child_count;
    if (_children) {
        true_count= (int)[_children count];
    }
    
    NSString* data = [NSString stringWithFormat:@"<id=%@ prev=%i next=%i name=%@ value=%@ nch=%i  nchd_t=%i t=%i l=%i w=%i h=%i/>\n", _unique_id,_prev_sibling, _next_sibling, _name, _value, _child_count, true_count , _top, _left, _width, _height];

    return data;
}

- (void) printStates {
    NSLog(@"states of %@   disable:%li selected:%li focused:%li pressed:%li checked:%li readonly:%li default:%li expanded:%li collapsed:%li busy:%li invisible:%li visited:%li linked:%li haspopup:%li protected:%li offscrn:%li selectable:%li focusable:%li", _name,
          _states & STATE_DISABLED,
          _states & STATE_SELECTABLE,
          _states & STATE_FOCUSED,
          _states & STATE_PRESSED,
          _states & STATE_CHECKED,
          _states & STATE_READONLY,
          _states & STATE_DEFAULT,
          _states & STATE_EXPANDED,
          _states & STATE_COLLAPSED,
          _states & STATE_BUSY,
          _states & STATE_INVISIBLE,
          _states & STATE_VISITED,
          _states & STATE_LINKED,
          _states & STATE_HASPOPUP,
          _states & STATE_PROTECTED,
          _states & STATE_OFFSCREEN,
          _states & STATE_SELECTABLE,
          _states & STATE_FOCUSABLE);
}

@end
