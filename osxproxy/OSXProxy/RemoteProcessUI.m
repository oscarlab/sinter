/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    RemoteControl.m
    Hello

    Created by Syed Masum Billah on 7/12/14.
*/

#import "RemoteProcessUI.h"
#import "ControlTypes.h"

@implementation RemoteProcessUI

- (BOOL) isEqualToUI:(id) object{
    RemoteProcessUI* other = object;
    if (!object) {
        return NO;
    }
    if(![object isKindOfClass:[RemoteProcessUI class]]){
        return NO;
    }
    
    if (_top != other.top || _left != other.left || _width!= other.width || _height !=other.height || _child_count !=  other.child_count || ![_name isEqualToString:other.name] || ![_value isEqualToString:other.value] || ![_process_id isEqualToString:other.process_id]) {
        return NO;
    }
    return YES;
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
