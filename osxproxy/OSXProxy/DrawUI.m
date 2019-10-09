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
    DrawUI.m
    NVRDP

    Created by Syed Masum Billah on 11/6/15.
*/

#import "DrawUI.h"
#import "Config.h"
#import "XMLTags.h"


@import Foundation;

@implementation DrawUI

@synthesize selectedMenuModel;
@synthesize focusedModel;
@synthesize rmUiRoot;

// static class variables
static int L;
static  ClientHandler  * sharedConnection;

#pragma mark Init
- (id) initWithProcessID:(NSString*) pid andIDTable:(NSMutableDictionary*) _idTable idToUITable:(NSMutableDictionary*) _idToUITable screenMapTable:(NSMutableDictionary*) _screenMapTable havingRemoteRootUI:(Model*) _rmRoot andWindow:(NSWindow*) win{
    self = [super init];
    if (self) {
        idTable     = _idTable;
        idToUITable = _idToUITable;
        screenMapTable = _screenMapTable;
        process_id  = pid;
        window      = win;
        lasOperationTime = [NSDate date];
        
        L =  ceil((log2(INT32_MAX))/2);
        sharedConnection = [ClientHandler getConnection];
        
        rmUiRoot = _rmRoot;
        localWinFrame = [window frame];
        
        condition = [[NSCondition alloc] init];
        
        NSMutableCharacterSet* tempSet =[[NSMutableCharacterSet alloc] init];
        [tempSet formUnionWithCharacterSet:[NSCharacterSet decimalDigitCharacterSet]];
        [tempSet formUnionWithCharacterSet:[NSCharacterSet whitespaceCharacterSet]];
        [tempSet addCharactersInString:@"e+-."];
        numeric = [tempSet invertedSet];
        
    }
    return self;
}

#pragma mark Utility functions
+ (NSImage *) getIconForString:(NSString*) title {
    NSImage * icon = nil;
    if ([[title lowercaseString] hasPrefix:@"back"])
        icon = [NSImage imageNamed:@"left_arrow.png"]; //NSImageNameGoLeftTemplate
    if ([[title lowercaseString] hasPrefix:@"forward"])
        icon = [NSImage imageNamed:@"right_arrow.png"];
    if ([[title lowercaseString] hasPrefix:@"refresh"])
        icon = [NSImage imageNamed:NSImageNameRefreshTemplate];
    if ([[title lowercaseString] hasPrefix:@"help"])
        icon = [NSImage imageNamed:NSImageNameInfo];
    if ([[title lowercaseString] hasPrefix:@"previous"]) //previous location
        icon = [NSImage imageNamed:@"down_arrow_small.png"];
    if ([[title lowercaseString] hasPrefix:@"recent"]) // recent location
        icon = [NSImage imageNamed:@"down_arrow_tiny.png"];
    if ([[title lowercaseString] hasPrefix:@"up"])  // up location
        icon = [NSImage imageNamed:@"up_arrow.png"];
    if (icon) {
        [icon setAccessibilityDescription:title];
    }
    return icon;
}

+ (Model*) getParentOf:(Model*) model havingRole:(NSString *) role {
    Model* temp = model;
    // search for parent role
    while (temp && ![temp.type isEqualToString:role]) {
        temp = temp.parent;
    }
    if(temp)
        return temp;
    
    return nil;
}

+ (NSInteger) getTagForControl:(Model*) model {
    int x = [model left]+[model width]/2;
    int y = [model top]+ [model height]/2;
    return ( x << L | y);
}

- (NSRect) getLocalFrameForRemoteFrame:(Model*)child {
    NSRect uiFrame = localWinFrame;
    uiFrame.origin.x = (child.left - rmUiRoot.left); //offset
    uiFrame.origin.y = localWinFrame.size.height -  (child.height+ (child.top - rmUiRoot.top));
    uiFrame.size.height = child.height;
    uiFrame.size.width = child.width;
    
    return uiFrame;
}

- (void) makeVisible:(Model*) model {
    NSView* view = [idToUITable objectForKey:model.unique_id];
    if (view) {
        [view setHidden:NO];
    }
}

- (void) hideAllViewsUnderUI:(Model*) control {
    NSRect rect;
    if(![self hasOverlappingPeer:control outParam:&rect]){
        return;
    }

    //NSString* unique_id = control.unique_id;
    NSMutableArray * values = [screenMapTable objectForKey:[NSValue valueWithRect:rect]];
    if (!values) return;
    
    NSView * view;
    //RemoteProcessUI* ui;
    BOOL isHidden = YES;
    // go over all runtime_id
    for (NSString* key in values) {
        view  = [idToUITable objectForKey:key];
        //ui    = [idTable objectForKey:key];
        
        /*
        if ([key isEqualToString:unique_id])
            isHidden = NO;
        else
            isHidden = YES;
        */
        if (view)
            [view setHidden:isHidden];
        /*
        if (ui) {
            if (isHidden)
                ui.states |= STATE_INVISIBLE;
            else
                ui.states &= ~STATE_INVISIBLE;
        }
         */
    }
}

- (Model*) getPseudoContainerOf:(Model*) child{
    NSRect rect;
    if ([self hasOverlappingPeer:child outParam:&rect]) {
        NSArray* ids = [screenMapTable objectForKey: [NSValue valueWithRect:rect]];
        NSString * unique_id = ids ? [ids firstObject] : nil;
        if (unique_id) {
            return [idTable objectForKey:unique_id];
        }
    }
    return nil;
}

- (BOOL) hasOverlappingPeer:(Model*) control outParam:(NSRect*) overlappingRect{
    NSRect rect1, rect2;
    rect1 = NSMakeRect(control.left, control.top, control.width, control.height);
    
    BOOL found = NO;
    for (NSValue* _rect2 in [screenMapTable allKeys]) {
        rect2 = [_rect2 rectValue];
        if(NSContainsRect (rect1, rect2) || NSContainsRect (rect2, rect1)){
            *overlappingRect = rect2;
            found = YES;
            break;
        }
    }
    return found;
}

// dictionaryTable: < rect, [unique_id1, unique_id2, ..] >
- (void) addToScreenMapTable:(Model*) control {
    NSValue * rect1 = [NSValue valueWithRect:
       NSMakeRect(control.left, control.top, control.width, control.height)];

    NSRect rect2;
    NSMutableArray *values;
    if([self hasOverlappingPeer:control outParam:&rect2]){
        values = [screenMapTable objectForKey:[NSValue valueWithRect:rect2]];
        if(values && ![values containsObject:control.unique_id]){
            [values addObject:control.unique_id];
        }
    }else{ // first-time
        values = [[NSMutableArray alloc] init];
        [values addObject:control.unique_id];
        [screenMapTable setObject:values forKey:rect1];
    }
}

// remove 'model' in its parent and returns its previous index
- (NSInteger) removeModel:(Model*) model{
    Model* parent = [model parent];
    NSInteger index = [[parent children] indexOfObject:model];
    [[parent children] removeObject:model];
    if (!model.isDuplicate) {
        [idTable removeObjectForKey:model.unique_id];
    }
    parent.child_count--;
    index--;
    return index;
}

- (BOOL) filterChildrenIn:(Model*) control validTypes:(NSArray*) types{
    Model* child;;
    BOOL filterApplied = NO;
    
    for (int i = 0; i < control.child_count; i++){
        child = control.children[i];
        
        // remove unwanted ui such as dialog, panel, etc.
        if(![types containsObject:child.type]){
            i = (int)[self removeModel:child];
            filterApplied = YES;
        }
    }
    return filterApplied;
}

/*
// This function will parse the "value" field where we have sent metadata for
// each word
//-(void) drawEditTextForWord:(RemoteProcessUI*) control frame:(NSRect)frame {
    //NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:frame] ;
    //    NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:[[self.window contentView] frame ]] ;
    //    int widthCorrection  = 100;
    //    int heightCorrection = 170;//220; for ipad
    //    NSSize contentSize = [[self.window contentView] frame].size;
    //    NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:NSMakeRect(widthCorrection,0,contentSize.width-2*widthCorrection,contentSize.height-heightCorrection)] ;
    //
    //    contentSize = [scrollview contentSize];
    //
    //    [scrollview setBorderType:NSNoBorder];
    //    [scrollview setHasVerticalScroller:YES];
    //    [scrollview setHasHorizontalScroller:NO];
    //    [scrollview setAutoresizingMask:NSViewWidthSizable |
    //     NSViewHeightSizable];
    //
    //    //NSTextView *theTextView = [[NSTextView alloc] initWithFrame:frame];
    //    theTextView = [[NSTextView alloc] initWithFrame:NSMakeRect(0, 0, contentSize.width, contentSize.height)];
    //    [theTextView setMinSize:NSMakeSize(0.0, contentSize.height)];
    //    [theTextView setMaxSize:NSMakeSize(contentSize.width, FLT_MAX)];
    //    [theTextView setVerticallyResizable:YES];
    //    [theTextView setHorizontallyResizable:NO];
    //    [theTextView setAutoresizingMask:(NSViewWidthSizable | NSViewHeightSizable)];
    //
    //    [[theTextView textContainer] setContainerSize:NSMakeSize(contentSize.width, FLT_MAX)];
    //    [[theTextView textContainer] setWidthTracksTextView:NO];
    //
    //    [scrollview setDocumentView:theTextView];
    //    [self.window.contentView addSubview:scrollview];
    //    [self.window makeKeyAndOrderFront:nil];
    //    [self.window makeFirstResponder:theTextView];
    //
    //
    //    MsWordTextParser *tempParser = [[MsWordTextParser alloc] init];
    //    [tempParser parse:[control.value dataUsingEncoding:NSUTF8StringEncoding]];
    //
    //
    //    for (MsWordTextElement *obj in tempParser.textElements)
    //    {
    //        if (obj == tempParser.textElements.lastObject)
    //            break;
    //
    //        NSFontTraitMask fontTrait = 0;
    //        if (obj.isBold)
    //        {
    //            fontTrait |= NSBoldFontMask;
    //        }
    //        if (obj.isItalic)
    //        {
    //            fontTrait |= NSItalicFontMask;
    //        }
    //
    //        NSFontManager *fontManager = [NSFontManager sharedFontManager];
    //        NSFont *customFont = [fontManager fontWithFamily:@"Verdana"
    //                                                  traits:fontTrait
    //                                                  weight:0
    //                                                    size:20.0f];
    //        NSDictionary *attributes = @{NSFontAttributeName : customFont};
    //
    //        NSString *text_to_show = [obj.text stringByReplacingOccurrencesOfString:@"\\r" withString:@"\n"];
    //        text_to_show = [text_to_show  stringByReplacingOccurrencesOfString:@"\\t" withString:@"    "];
    //        text_to_show = [text_to_show  stringByReplacingOccurrencesOfString:@"\\'" withString:@"'"];
    //
    //        NSMutableAttributedString *attrstr = [[NSMutableAttributedString alloc] initWithString:text_to_show];
    //        [attrstr setAttributes:attributes range:NSMakeRange(0, text_to_show.length)];
    //        if (obj.isUnderline)
    //        {
    //            [attrstr addAttribute:NSUnderlineStyleAttributeName value:[NSNumber numberWithInt:NSUnderlineStyleSingle] range:NSMakeRange(0, text_to_show.length)];
    //        }
    //        [[theTextView textStorage] appendAttributedString: attrstr];
    //    }
    //
    //    NSRange zeroRange = { 0, 0 };
    //    [theTextView setSelectedRange: zeroRange];
    
//}

//- (BOOL)textView:(NSTextView *)aTextView shouldChangeTextInRange:(NSRange)affectedCharRange replacementString:(NSString *)replacementString {
//    return TRUE;
//}
//
//- (BOOL)textView:(NSTextView *)aTextView doCommandBySelector:(SEL)aSelector {
//    return TRUE;
//}

//-(void) drawTerminal:(RemoteProcessUI*) control frame:(NSRect)frame {
//    if (![idToUITable objectForKey:[self generateKey:control]])
//    {
//        NSSize contentSize = [[self.window contentView] frame].size;
//        NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:NSMakeRect(0,0,contentSize.width,contentSize.height)] ;
//        
//        contentSize = [scrollview contentSize];
//        
//        [scrollview setBorderType:NSNoBorder];
//        [scrollview setHasVerticalScroller:YES];
//        [scrollview setHasHorizontalScroller:NO];
//        [scrollview setScrollsDynamically:YES];
//        [scrollview setAutoresizingMask:NSViewWidthSizable |
//         NSViewHeightSizable];
//        
//        theTextView = [[NSTextView alloc] initWithFrame:NSMakeRect(0, 0, contentSize.width, contentSize.height)];
//        [theTextView setMinSize:NSMakeSize(0.0, contentSize.height)];
//        [theTextView setMaxSize:NSMakeSize(contentSize.width, FLT_MAX)];
//        [theTextView setVerticallyResizable:YES];
//        [theTextView setHorizontallyResizable:NO];
//        //[theTextView setBackgroundColor:[NSColor blackColor]];
//        //[theTextView setTextColor:[NSColor whiteColor]];
//        
//        [theTextView setAutoresizingMask:(NSViewWidthSizable | NSViewHeightSizable)];
//        [theTextView setEditable:TRUE];
//        [[theTextView textContainer] setContainerSize:NSMakeSize(contentSize.width, FLT_MAX)];
//        [[theTextView textContainer] setWidthTracksTextView:NO];
//        [theTextView setDelegate:self];
//        
//        
//        [scrollview setDocumentView:theTextView];
//        [self.window.contentView addSubview:scrollview];
//        [self.window makeKeyAndOrderFront:nil];
//        [self.window makeFirstResponder:theTextView];
//        
//        
//        
//        //[theTextView setString: [control.value stringByReplacingOccurrencesOfString:@"\r" withString:@"\n"]];
//        
//        [idToUITable setObject:theTextView forKey:[self generateKey:control]];
//        NSString *mesg = [NSString stringWithFormat:@"kb:%@ chars:%@\n", remoteProcess.id, @"leftcontrol+c"];
//        [sharedConnection sendMessage:mesg];
//    }
//}



//
//-(void) drawTab:(RemoteProcessUI*) control frame:(NSRect)frame {
//
//    NSTabViewItem *item = [[NSTabViewItem alloc]
//                           init];
//    [item setLabel:control.name];
//
//    [[item view] setAutoresizesSubviews:TRUE];
//
//    NSSize contentSize = frame.size;
//    NSView *tempView = [[NSView alloc] initWithFrame:NSMakeRect(0,0,FLT_MAX,contentSize.height)] ;
//
//    NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:NSMakeRect(0,0,contentSize.width,contentSize.height)] ;
//
//
//    [scrollview setBorderType:NSBezelBorder];
//    [scrollview setHasVerticalScroller:YES];
//    [scrollview setHasHorizontalScroller:YES];
//    [scrollview setScrollsDynamically:YES];
//    [scrollview setAutoresizingMask:NSViewWidthSizable|NSViewHeightSizable];
//    [scrollview setDocumentView:tempView];
//
//
//    [item setView:scrollview];
//
//    [tabView addTabViewItem:item];
//
//    //    [tabView setAutoresizesSubviews:TRUE];
//    //    [tabView setAutoresizingMask:(NSViewWidthSizable | NSViewHeightSizable)];
//    //
//    [idToUITable setObject:item forKey:control.name];
//}
//
//
//- (void)tabView:(NSTabView *)tabView1 didSelectTabViewItem:(NSTabViewItem *)tabViewItem
//{
//}
//
//- (bool)tabView:(NSTabView *)tabView1 shouldSelectTabViewItem:(NSTabViewItem *)tabViewItem
//{
//    NSLog (@"shouldSelectTabViewItem");
//    return YES;
//}
//
//- (void)tabView:(NSTabView *)tabView willSelectTabViewItem:(NSTabViewItem *)tabViewItem
//{
//    NSLog (@"willSelectTabViewItem");
//}
//
//// MARK: STYLE PANEL
//-(void) drawStylesPanel:(RemoteProcessUI*) control frame:(NSRect)frame isGroup:(BOOL) present {
//
//    NSLog(@"In drawStyles control.name=%@ control.child_count=%i", control.name, control.child_count);
//
//    NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:frame] ;
//    NSSize contentSize = [scrollview contentSize];
//    RemoteProcessUI* child = control.children[0];
//
//    NSLog(@"In drawStyles control.name=%@ control.child_count=%i child_role:%i contentSize.height =%f", child.name, child.child_count, child.role, contentSize.height);
//
//    [scrollview setBorderType:NSNoBorder];
//    [scrollview setHasVerticalScroller:YES];
//    [scrollview setHasHorizontalScroller:NO];
//    [scrollview setAutoresizingMask:NSViewWidthSizable |
//     NSViewHeightSizable];
//
//    NSTableView* tableView = [[NSTableView alloc] initWithFrame:NSMakeRect(0, 0, contentSize.width-2, contentSize.height)];
//
//    [tableView setAutoresizingMask:NSViewWidthSizable];
//    [tableView setRowHeight:100];
//    [tableView setIntercellSpacing:NSMakeSize(3, 2)];
//    [tableView setIdentifier:@"styleTable"];
//
//    //computing number of columns
//    RemoteProcessUI* list_item;
//    for (int i = 0; i < child.child_count;i++)
//    {
//        list_item = child.children[i];
//        NSLog(@"Children index:%i role:%i name:%@", i, list_item.role, list_item.name);
//        if (list_item.role == ROLE_LISTITEM) {
//
//            NSTableColumn* col = [[NSTableColumn alloc] initWithIdentifier:list_item.name];
//            [col setWidth:list_item.width];
//            [[col headerCell] setStringValue:list_item.name];
//            [[col dataCell] setControlSize:NSMiniControlSize];
//            [tableView addTableColumn:col];
//            [self drawButton:list_item frame:tableView.frame isGroup:YES ];
//        }
//    }
//    [tableView setHeaderView:nil]; //sets header of table to null
//
//
//    //register delegates
//    //    if (![idToUITable objectForKey:control.process_id]) {
//    if (![idToUITable objectForKey:tableView.identifier]) {
//        [idToUITable setObject:tableView forKey:tableView.identifier];
//    }
//    [tableView setDelegate:self];
//    [tableView setDataSource:self];
//
//    //add to scrollView
//    [scrollview setDocumentView:tableView];
//
//
//    if(present)
//    {
//        NSRect uiFrame = [self getHostFrameForTab:control frame:frame];
//        [scrollview setFrame:uiFrame];
//        //////[button sizeToFit];
//        NSArray *views = [[[[tabView selectedTabViewItem] view] subviews][0] subviews];
//        if (views)
//        {
//            bool overlap;
//            do
//            {
//                overlap = false;
//                int i;
//                for (i = 0; i < views.count; i++)
//                {
//                    NSRect rec2 = [views[i] frame];
//                    if ([self isOverlapping:uiFrame rec2:rec2])
//                    {
//                        uiFrame.origin.x = rec2.origin.x + rec2.size.width + 1;
//                        [scrollview setFrame:uiFrame];
//                        overlap = true;
//                        break;
//                    }
//                }
//            }while (overlap);
//        }
//
//        //[[[tabView selectedTabViewItem] view]  addSubview:scrollview];
//
//        NSView *tabScrollView =  [[[tabView selectedTabViewItem] view] subviews][0];
//        [tabScrollView addSubview:scrollview];
//    }
//    else
//        [self.window.contentView addSubview:scrollview];
//
//    //update shared next index
//    index = [self getNextSibling:control];
//
//    //now load tableview
//    [tableView reloadData];
//
//}
//
*/

#pragma mark NSTextView/EDIT
- (NSTextView *) drawEditText:(Model*) model frame:(NSRect)frame parentView:(NSView*) parent type:(NSString*)uiType{
    CustomTextView *textView = [idToUITable objectForKey:model.unique_id];
    
    NSString* string = nil;
	string = model.value;
    
    if (textView) {
        
        [textView setString: string];
        if (model.version) {
            model.version = 0;
        }
        return textView;
    }
    
    NSScrollView *scrollview = [[NSScrollView alloc] initWithFrame:frame] ;
    NSSize contentSize = [scrollview contentSize];
    
    [scrollview setBorderType:NSNoBorder];
    [scrollview setHasVerticalScroller:YES];
    [scrollview setHasHorizontalScroller:YES];
    [scrollview setAutoresizingMask:NSViewWidthSizable |
     NSViewHeightSizable];
    
    //textView = [[NSTextView alloc] initWithFrame:NSMakeRect(0, 0, contentSize.width-2, contentSize.height)];
    textView = [[CustomTextView alloc] initWithFrame:NSMakeRect(0, 0, contentSize.width-2, contentSize.height)
               andConnection:sharedConnection];
    textView.process_id = process_id;
    
    [textView setMinSize:NSMakeSize(0.0, contentSize.height)];
    [textView setMaxSize:NSMakeSize(FLT_MAX, FLT_MAX)];
    [textView setVerticallyResizable:YES];
    [textView setHorizontallyResizable:NO];//
    [textView setAutoresizingMask:NSViewWidthSizable];//|NSViewHeightSizable
    
    [[textView textContainer]
     setContainerSize:NSMakeSize(contentSize.width, FLT_MAX)];
    [[textView textContainer] setWidthTracksTextView:YES];
    [textView setDelegate:(id<NSTextViewDelegate>)self];
    
    // assign id before adding text
    [textView setIdentifier:model.unique_id];
    [idToUITable setObject:textView forKey:textView.identifier];

    
    [textView setString: string]; //stringByReplacingOccurrencesOfString:@"\r" withString:@"\n"]
    [scrollview setDocumentView:textView];
    [parent addSubview:scrollview];//
    
    // full screen
    //[(NSWindow*) parent setContentView:textView];
    //[(NSWindow*) parent makeKeyAndOrderFront:nil];
    //[(NSWindow*) parent makeFirstResponder:textView];

    //set cursor to beginning just like newly open a file
    [textView setSelectedRange:(NSRange){0, 0}];
    [sharedConnection sendKeystrokes:@"^{HOME}" processId:process_id targetId:textView.identifier];
    
    return textView;
    
}

- (NSTextField *) drawSimpleEditText:(Model*) model frame:(NSRect)frame parentView:(NSView*) parent{
    if (model.states & STATE_INVISIBLE) {
        return nil;
    }
    
    NSTextField *textField = [idToUITable objectForKey:model.unique_id];
    
    if (textField) {
        [textField setStringValue:model.value];

        // bring to front
        //[textView removeFromSuperview];
        //[parent addSubview:textView positioned:NSWindowAbove relativeTo:nil];
        if (model.version) {
            model.version = 0;
        }
        return textField;
    }
    
    textField = [[NSTextField alloc] initWithFrame:frame];
    [textField setStringValue: model.value];
    
    [textField setEditable:YES];
    [textField setAutoresizesSubviews:TRUE];
    [parent addSubview:textField];
    [[textField window] makeFirstResponder:textField];
    [textField setNeedsDisplay:YES];
    
    [textField setDelegate:(id<NSTextFieldDelegate>) self];
    [textField setIdentifier:model.unique_id];
    [idToUITable setObject:textField forKey:textField.identifier];
    
    return textField;
}

#pragma mark NSTextField/ControlView Delegate
- (void)controlTextDidChange:(NSNotification *)notification {
    //NSTextView *textView = notification.userInfo[@"NSFieldEditor"];
    NSTextField * textField = [notification object];
    [sharedConnection setTextAt:textField.identifier text:[textField stringValue] processId: process_id];
    //NSLog(@"text-field %@", [textField stringValue]);
}

/*
 //doesn't require now, useful during autocompletion
 - (BOOL)control:(NSControl *)control textView:(NSTextView *)textView doCommandBySelector:(SEL)commandSelector{
 return YES;
 }
 - (NSArray *)control:(NSControl *)control textView:(NSTextView *)textView completions:(NSArray *)words forPartialWordRange:(NSRange)charRange indexOfSelectedItem:(NSInteger *)index {
 return nil;
 }
 */

- (void)controlTextDidEndEditing:(NSNotification *)aNotification {
    if ([[aNotification object] isKindOfClass:[NSComboBox class]]) {
        NSComboBox* combobox = [aNotification object];
        //NSLog(@"combobox text %@", [combobox stringValue]);
        
        Model* control = [idTable objectForKey:combobox.identifier];
        if (control && control.user_data){//
            [sharedConnection setTextAt:[control.user_data objectForKey:@"edit"] text:[combobox stringValue] processId: process_id ];
        }
    }
}


#pragma mark My Custom Label
- (BOOL) isNumericString:(NSString*) value{
    return [value rangeOfCharacterFromSet:numeric].location == NSNotFound;
}

-(void)speakString:(NSString *)stringToSpeak fromFocusedUIElement:(id)object {
    NSDictionary *announcementInfo = [[NSDictionary alloc] initWithObjectsAndKeys:
                                      stringToSpeak, NSAccessibilityAnnouncementKey,
                                      @"High", NSAccessibilityPriorityKey, nil];
    NSAccessibilityPostNotificationWithUserInfo(object, NSAccessibilityAnnouncementRequestedNotification, announcementInfo);
    
}
                                      
-(CustomLabel*) drawText:(Model*) model frame:(NSRect)frame parentView:(NSView*) parent{
    if (model.states & STATE_INVISIBLE) {
        return nil;
    }
    
    CustomLabel *label = [idToUITable objectForKey:model.unique_id];
    if (label) {
//        if(model.states & STATE_FOCUSED ||
//           ![model.name isEqualToString:model.value]){
//            [label setHidden:NO];
//        } else {
//            [label setHidden:YES];
//        }
        [label setHidden:NO];
        [label setStrValue:model.value];
        
        if ([model.value isEqualToString:@"Memory"] && [rmUiRoot.name isEqualToString:@"Calculator"]){
            [label setStrValue:@""]; /* tweak for windows calculator @"Calc" */
        }
        
        //[label setLabel:model.name];
        if (model.version) {
            model.version = 0;
        }
        return label;
    }
    label = [[CustomLabel alloc] initWithFrame:frame andConnection:sharedConnection];
    [label setLabel:model.name];
    [label setStrValue: model.value];
    [label setHidden:(model.states & STATE_INVISIBLE)?YES:NO];
    // check if it is not numeric string
    if (![self isNumericString:model.name]){
        [label setStrValue: model.name];
    }

    [[label window] makeFirstResponder:label];
    [label setAutoresizesSubviews:TRUE];
    if (![parent isKindOfClass:[NSMenu class]]) //to avoid exception thrown
    {
        [parent addSubview:label];
    }
    
    [label setIdentifier:model.unique_id];
    [idToUITable setObject:label forKey:label.identifier];
    
    if ([[window title] hasPrefix:@"Calc"]) { //[rmUiRoot.name isEqualToString:@"Calculator"]
        if(model.states & STATE_FOCUSED){
            [label setLabel:@"Result"];
            [label setNeedsDisplay:YES];
        }else{
            if (![model.name isEqualToString:@"Running History"] && ![model.name isEqualToString:@"Memory"]){
                //[label setHidden:YES];//ignore the other result text field from windows calculator
            }
            if ([model.name isEqualToString:@"Memory"]){
                [label setStrValue:@""]; /* tweak for windows calculator @"Calc" */
            }
        }
        return label;
    }

    [label setNeedsDisplay:YES];
    return label;
}

-(CustomLabel*) drawLabel:(Model*) model frame:(NSRect)frame parentView:(NSView*) parent{
    if (model.states & STATE_INVISIBLE) {
        return nil;
    }
    
    CustomLabel *label = [idToUITable objectForKey:model.unique_id];
    if (label) {
        [label setHidden:NO];
        [label setStrValue:model.value];
        
        //[label setLabel:model.name];
        if (model.version) {
            model.version = 0;
        }
        return label;
    }
    label = [[CustomLabel alloc] initWithFrame:frame andConnection:sharedConnection];
    [label setLabel:model.name];
    [label setStrValue: model.value];
    
    // check if it is not numeric string
    if (![self isNumericString:model.value]){
        [label setStrValue: model.name];
    }
    
    [[label window] makeFirstResponder:label];
    [label setAutoresizesSubviews:TRUE];
    if (![parent isKindOfClass:[NSMenu class]]){ // to avoid exception thrown
        [parent addSubview:label];
    }
    
    [label setIdentifier:model.unique_id];
    [idToUITable setObject:label forKey:label.identifier];
    
    [label setNeedsDisplay:YES];
    return label;
}

#pragma mark NSTextViewDelegate
- (BOOL)textView:(NSTextView *)textView shouldChangeTextInRanges:(NSArray<NSValue *> *)affectedRanges replacementStrings:(nullable NSArray<NSString *> *)replacementStrings {
    
    if (![[replacementStrings firstObject] isEqualToString:@""]) {
        
        
        [sharedConnection sendActionMsg:process_id targetId:(textView.identifier) actionType:STRActionAppendText data:[replacementStrings firstObject]];
        //[sharedConnection sendKeystrokes:[replacementStrings firstObject] processId:process_id targetId:textView.identifier]; //this is what work for mac scraper now
        //return YES;
    }
    return YES ;
}

- (void)textViewDidChangeSelection:(NSNotification *)notification {
    CustomTextView* textView = [notification object];
    NSRange selectedRange = textView.selectedRange;
    //NSLog(@"current range <%lu, %lu>", selectedRange.length, selectedRange.location);
    [sharedConnection sendCaretMoveAt:textView.identifier andLocation:selectedRange.location andLength:selectedRange.length];
    /*
    NSRange effectiveRange;
    if(selectedRange.length > 0) {
        NSLog(@" %@ ",[[textView textStorage] attributesAtIndex:selectedRange.location
                                               longestEffectiveRange:&effectiveRange inRange:selectedRange]);
    }*/
    //NSLog(@"I'm here ");
}

/*
- (void)textViewDidChangeTypingAttributes:(NSNotification *)notification {
    NSLog(@"I'm here too");
}

- (void) textDidChange:(NSNotification *)aNotification {
    //NSLog(@"typed string" );
}
*/


#pragma mark NSSearchBox
- (NSSearchField *) drawSearchField:(Model*) model frame:(NSRect)frame parentView:(NSView*) parent {
    
    // go to searchbox children
    Model * child = nil;
    for (int i=0; i < model.child_count; i++) {
        child = model.children[i];
        if ([child.type caseInsensitiveCompare:@"edit"] == NSOrderedSame){
            break;
        }
    }
    if (!child) {
        return nil;
    }
    // check existing
    NSSearchField *searchField;
    if ((searchField  = [idToUITable objectForKey:child.unique_id])) {
        model.version--;
        return searchField;
    }
    // create new
    searchField = [[NSSearchField alloc] initWithFrame:frame];
    //[searchField setT]
    [searchField setAutoresizesSubviews:TRUE];
    [searchField setSendsSearchStringImmediately:NO];
    [searchField setSendsWholeSearchString:YES];
    [searchField setDelegate:(id<NSSearchFieldDelegate>) self];
    // id
    [searchField setIdentifier:child.unique_id];
    // add to parent view
    [parent addSubview:searchField];
    [searchField setNeedsDisplay:YES];
    
    // add to cache
    [idToUITable setObject:searchField forKey:searchField.identifier];
    
    return searchField;
}

# pragma mark NSSearchField Delegate
- (void)searchFieldDidEndSearching:(NSSearchField *)sender {
    [sharedConnection setTextAt:[sender identifier] text:[sender stringValue] processId: process_id];
}

#pragma mark Group
- (void) drawGroup:(Model*) control frame:(NSRect)frame {
    if (control.child_count == 0){
        [self addToScreenMapTable:control];
    } else {
        for (int i = 0;i<control.child_count; i++){
            //[self drawItem:control.children[i] isGroup:YES];
        }
    }
}

- (NSView *) drawEmptyView: (Model*) control frame:(NSRect)frame parentView:(NSView *) parent {
    NSView * view = [[NSView alloc] initWithFrame:frame];
    [view setIdentifier:control.unique_id];
    [view setNeedsDisplay:YES];
    [self addToScreenMapTable:control];
    return view;
}

#pragma mark NSButton
- (NSButton * ) drawButton:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent{
    NSButton * button = [idToUITable objectForKey:control.unique_id];
    if (button) {
        button.title = control.name;
        return button;
    }
    
    button = [[NSButton alloc] initWithFrame:frame];
    if ([control.name length]) {
        button.title = control.name;
    }else if ([control.value length]){
        button.title = control.value;
    }else{
    }
    
    [button setAutoresizesSubviews:TRUE];
    [button setButtonType:NSMomentaryLightButton];
    [button setBezelStyle:NSTexturedSquareBezelStyle];
    [button setTarget:self];
    [button setAction:@selector(sendAction:)];
    [button setIdentifier:control.unique_id];
    [button setEnabled: (control.states & STATE_DISABLED) == STATE_DISABLED? NO : YES];
    //[button setFont: [NSFont systemFontOfSize: 10]];
    [parent addSubview:button];
    
    [idToUITable setObject:button forKey:button.identifier];
    [button setNeedsDisplay:YES];
    return button;
}


- (void) populateRadioButtonCell:(NSMatrix*) matrix newCell:(Model*) cell{
    NSRect rect = [matrix frame];
    if (rect.size.height > rect.size.width) { // add rows
        [matrix addRow];
    } else{ // add columns
        [matrix addColumn];
    }
    NSCell* current = [[matrix cells] lastObject];
    [current setTitle:cell.name];
    [current setRepresentedObject:cell.unique_id];
    
    if (cell.states & STATE_SELECTED) {
        [matrix selectCell:current];
    }
}

- (NSView * ) drawRadioButton:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent{
    NSRect rect;
    Model *pseudoContainer = [self getPseudoContainerOf:control];
    if (pseudoContainer) {
        NSMatrix *myMatrix  = [idToUITable objectForKey:pseudoContainer.unique_id];
        if (myMatrix) {
            [self populateRadioButtonCell:myMatrix newCell:control];
        } else {
            NSButtonCell *prototype = [[NSButtonCell alloc] init];
            [prototype setButtonType:NSRadioButton];
            rect = [self getLocalFrameForRemoteFrame:pseudoContainer];
            myMatrix = [[NSMatrix alloc] initWithFrame:rect mode:NSRadioModeMatrix
                         prototype:(NSCell *)prototype numberOfRows:0 numberOfColumns:0];
            //settext, and id
            [self populateRadioButtonCell:myMatrix newCell:control];
            
            [myMatrix setIdentifier:pseudoContainer.unique_id];
            [idToUITable setObject:myMatrix forKey:myMatrix.identifier];
            
            [parent addSubview:myMatrix];
            [myMatrix setNeedsDisplay:YES];
            [myMatrix setTarget:self];
            [myMatrix setAction:@selector(radioButtonAction:)];
        }
        return myMatrix;
    }
    else { // add regular round button
        NSButton* button = [self drawButton:control frame:frame parentView:parent];
        [button setBezelStyle:NSCircularBezelStyle];
        [button setButtonType:NSOnOffButton];
        if (control.states & STATE_SELECTED) {
            [button setState:NSOnState];
        }else{
            [button setState:NSOffState];
        }
        [button setAction:@selector(sendToggleAction:)];
        return button;
    }
}

- (IBAction) radioButtonAction:(id)sender { // sender is NSMatrix object
    NSButtonCell *selCell = [sender selectedCell];
    //NSLog(@"Selected cell is %@", [selCell representedObject]);
    [sharedConnection sendActionMsg:process_id targetId:[selCell representedObject] actionType:STRActionSelect data:nil];
}

- (NSButton * ) drawCheckBox:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent{
    NSButton *button = [self drawButton:control frame:frame parentView:parent];

    [button setButtonType:NSSwitchButton];
    [button setBezelStyle:NSTexturedSquareBezelStyle];
    if (control.states & STATE_CHECKED) {
        [button setState:NSOnState];
    } else {
        [button setState:NSOffState];
    }
    
    [button setAction:@selector(sendToggleAction:)];
    return button;
}

- (IBAction) sendToggleAction:(id) sender {
    NSString* unique_id  = [(NSControl *) sender identifier];
    if (unique_id ) {
        if ([idTable objectForKey:unique_id]) {
            [sharedConnection sendActionMsg:process_id targetId:unique_id actionType:STRActionToggle data:nil];
        }
    }
}


#pragma mark SplitButton/PopUpButton/PullDownList/MenuButton
- (NSPopUpButton * ) drawMenuButton:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent {
    //    if (![idToUITable objectForKey:control.process_id]) {

    NSPopUpButton *button = [[NSPopUpButton alloc] initWithFrame:frame];
    if ([control.name length]) {
        button.title = control.name;
    }else if ([control.value length]){
        button.title = control.value;
    }else{}
    
    //[button setAutoresizesSubviews:TRUE];
    //[button setButtonType:NSMomentaryLightButton];
    //[button setBezelStyle:NSTexturedSquareBezelStyle];
    [button setTarget:self];
    [button setAction:@selector(sendAction:)];
    [button setPullsDown:YES];
    [button setIdentifier: control.unique_id];
    [button setAutoenablesItems:TRUE];
    [button setFont: [NSFont systemFontOfSize:[NSFont smallSystemFontSize]]];

    if (!control.child_count) {
        [button addItemWithTitle:control.name];
    } else {
        for (int i = 0 ; i < control.child_count ; i++){
            [button addItemWithTitle:[control.children[i] name]];
        }
    }
    
    [parent addSubview:button];
    [button setNeedsDisplay:YES];
    // add reference
    // [idToUITable setObject:button forKey:control.unique_id];
    return button;
}

#pragma mark Breadcrumb/addressBar
- (NSPathControl*) drawBreadCrumb:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent{
    if (!control.child_count)
        return nil;
    
    // find proper toolbar item
    Model* pathUI = nil;
    for (int i = 0 ; i < control.child_count ; i++){
        if ([((Model*)control.children[i]).type caseInsensitiveCompare:@"toolbar"] == NSOrderedSame) {
            pathUI = control.children[i];
            break;
        }
    }
    if (!pathUI ) return  nil;
   
    // check for existing view
    CustomPathControl* pathControl = [idToUITable objectForKey:pathUI.unique_id];
    if (pathControl) {
        [pathControl updadeWithRoot:pathUI];
        return pathControl;
    }

    // create a new one
    pathControl = [[CustomPathControl alloc] initWithFrame:frame model:pathUI andContainer:parent];

    //store references
    if (![idToUITable objectForKey:pathControl.identifier]) {
        [idToUITable setObject:pathControl forKey:pathControl.identifier];
    }
    
    [self addToScreenMapTable:pathUI];
    
    return pathControl;
}

#pragma mark SegmentedControl
- (void) populateSegmentsFromUI:(Model*) control frame:(NSRect) newFrame toSegmentedControl:(NSSegmentedControl*) segmentedControl {
    // update the frame
    [segmentedControl setFrame:newFrame];
    [segmentedControl setSegmentCount:control.child_count];

    Model* toolbarItem;
    for (int i = 0; i < control.children.count; i++) {
        toolbarItem = control.children[i];
        [segmentedControl setWidth:toolbarItem.width forSegment:i];
        [segmentedControl setImage:[DrawUI getIconForString:toolbarItem.name] forSegment:i];
        [[segmentedControl cell] setTag:i forSegment:i];
    }
}

- (void) updateSegmentedControl:(NSSegmentedControl *) segmentedControl frame:(NSRect) newFrame usingUI:(Model*) control{
    [self populateSegmentsFromUI:control frame:newFrame toSegmentedControl:segmentedControl];
    [segmentedControl setHidden:NO];
    [segmentedControl setNeedsDisplay:YES];
}

- (NSSegmentedControl*) drawSegmentedControl:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent {
    NSSegmentedControl* segmentedControl = [idToUITable objectForKey:control.unique_id];
    
    if (segmentedControl) {
        [self updateSegmentedControl:segmentedControl frame:frame usingUI:control];
        return segmentedControl;
    }
    
    // create a Segmented Control
    segmentedControl = [[NSSegmentedControl alloc] initWithFrame:frame];
    [segmentedControl  setIdentifier:control.unique_id];
    [[segmentedControl cell ] setTrackingMode:NSSegmentSwitchTrackingMomentary];
    [segmentedControl setTarget: self];
    [segmentedControl setAction:@selector(segmentedControlClicked:)];
    [self populateSegmentsFromUI:control frame:frame toSegmentedControl:segmentedControl];
    //[segmentedControl acceptsFirstResponder:YES];
    
    [parent addSubview:segmentedControl];
    [segmentedControl setNeedsDisplay:YES];
    
    //store references
    if (![idToUITable objectForKey:segmentedControl.identifier]) {
        [idToUITable setObject:segmentedControl forKey:segmentedControl.identifier];
    }
    [self addToScreenMapTable:control];
    
    return segmentedControl;
}

- (IBAction) segmentedControlClicked: (id) sender {
    int clickedSegment = (int)[sender selectedSegment];
    int clickedSegmentTag = (int)[[sender cell] tagForSegment:clickedSegment];
    Model* control = [idTable objectForKey:[(NSControl*) sender identifier]];
    if (control && clickedSegmentTag < control.child_count) {
        [sharedConnection sendActionMsg:process_id targetId:[control.children[clickedSegmentTag] unique_id] actionType:STRActionDefault data:nil];
    }
}


#pragma mark Toolbar
- (NSControl*) drawToolbar:(Model*) control frame:(NSRect) frame parentView:(NSView *) parent{
    NSControl* toolbarView = [idToUITable objectForKey:control.unique_id];
    if (toolbarView) {
        if ([toolbarView isKindOfClass:[NSPathControl class]]) {
            [(CustomPathControl*) toolbarView updadeWithRoot:control];
        }
        if ([toolbarView isKindOfClass:[NSSegmentedControl class]]) {
            [self updateSegmentedControl:(NSSegmentedControl*)toolbarView frame:frame usingUI:control];
        }
        //version -- ?
        return toolbarView;
    }
    
    // create a SegmentedControl
    toolbarView = [self drawSegmentedControl:control frame:frame parentView:parent];
    return toolbarView;
}

#pragma mark COMBO-BOX
- (void) setSelectedItem:(Model *) control comboBox: comboBox {
    if(control.user_data) {
        Model * list;
        for(Model * item in control.children){
            if([item.type isEqualToString:@"List"]){
                list = item;
                break;
            }
        }
        
        for(int i=0; i<list.child_count; ++i){
            Model * item = list.children[i];
            if((item.states & STATE_SELECTED) == STATE_SELECTED) {
                [comboBox selectItemAtIndex:i];
                break;
            }
        }
    }
}

- (NSComboBox *) drawComboBox:(Model*) control frame:(NSRect)frame parentView:(NSView*) parent {
    NSComboBox * comboBox = [idToUITable objectForKey:control.unique_id];
    if (comboBox) {
        BOOL hasList = [self updateComboBoxData:comboBox];
        [comboBox reloadData];
        if(hasList){
            [self setSelectedItem: control comboBox: comboBox];
        }
        [comboBox setHidden:NO];
        if (control.version) {
            control.version--;
        }
        return comboBox;
    }
    
    // create a new one
    comboBox = [[NSComboBox alloc] initWithFrame:frame];
    //[comboBox addItemWithObjectValue:control.value];

    
    // identifier
    [comboBox setIdentifier:control.unique_id];
    [comboBox setAutoresizesSubviews:NO];

    [comboBox setFont: [NSFont systemFontOfSize:[NSFont smallSystemFontSize]]];
    [(NSTextField*) comboBox setDelegate:(id<NSTextFieldDelegate>) self];

    [comboBox setUsesDataSource:YES];
    [comboBox setDataSource:self];
    [comboBox setHasVerticalScroller:YES];
    [comboBox setNumberOfVisibleItems:10];
    
    [parent addSubview:comboBox];
    [comboBox setNeedsDisplay:YES];
    
    // store reference
    if (![idToUITable objectForKey:comboBox.identifier]) {
        [idToUITable setObject:comboBox forKey:comboBox.identifier];
    }
    [self addToScreenMapTable:control];
    
    // request load
    [self updateComboBoxData:comboBox];
    [comboBox reloadData];
    //[comboBox selectItemAtIndex:0];
    [self setSelectedItem: control comboBox: comboBox];
    
    return comboBox;
}

- (id) comboBox:(NSComboBox *)aComboBox objectValueForItemAtIndex:(NSInteger) index {
    Model * control = [idTable objectForKey:aComboBox.identifier];
    if (control.user_data) {
        Model *list = [idTable objectForKey:[control.user_data objectForKey:@"list"]];
        if (list) {
            return [[[list children] objectAtIndex:index] name];
        }
    }
    return control.value;
    //[(NSTextField*) aComboBox setStringValue:name];

}
//Note: list-size is limited to 10 items due to cocoa problem
- (NSInteger)numberOfItemsInComboBox:(NSComboBox *)aComboBox {
    Model * control = [idTable objectForKey:aComboBox.identifier];
    if (control.user_data) {
        Model *list = [idTable objectForKey:[control.user_data objectForKey:@"list"]];
        if (list && list.child_count) {
            return list.child_count;
        }else{ //send request again
            //[self updateComboBoxData:aComboBox];
        }
    }
    return 1;
}

- (BOOL) updateComboBoxData:(NSComboBox *) aComboBox {
    BOOL hasList = NO;
    Model* control = [idTable objectForKey:aComboBox.identifier];
    if (!control)
        return hasList;
    // always make Combobox First Responder if possible
    [window makeFirstResponder:aComboBox];
    
    // store extra information in 'user_data'
    if (!control.user_data)
        control.user_data = [[NSMutableDictionary alloc] init];
    
    // find 'list', 'load button' in combobox
    for (Model* item in control.children) {
        if ([item.type caseInsensitiveCompare:@"edit"] == NSOrderedSame)
            [control.user_data setObject:item.unique_id forKey:@"edit"];

        if ([item.type caseInsensitiveCompare:@"button"] == NSOrderedSame)
            [control.user_data setObject:item.unique_id forKey:@"load"];
        
        if ([item.type caseInsensitiveCompare:@"list"] == NSOrderedSame && item.child_count) {
            [self getListHeader:item];
            [control.user_data setObject:item.unique_id forKey:@"list"];
            hasList = YES;
        }
    }
    // if list is not populated, expand the list
    if (!hasList) {
        for (int i=0; i < 1; i++) { //press the load button
            [sharedConnection sendActionMsg:process_id targetId:[control.user_data objectForKey:@"load"] actionType:STRActionDefault data:nil];
        }
    }
    return hasList;
}


#pragma mark ProgressBar
- (id) drawProgressBar:(Model*) control frame:(NSRect)frame parentView:(NSView *) parent{
    if (!control.child_count) {
        // a regular progressbar, follow the draw-routine
    } else { // complex object
        if(control.version){
            [self hideAllViewsUnderUI:control];
            control.version--;
        } else {
            [self addToScreenMapTable:control];
            // register structure change notification
            [sharedConnection sendActionMsg:process_id targetId:(control.unique_id) actionType:@"structureChangeNotification" data:nil];
            NSLog(@"what is \"structureChangeNotification\"??");
        }
    }
    return nil;
}

#pragma mark NSTableView/list BEGIN
- (Model*) getListHeader:(Model*) control{
    Model* list_item = nil;
    Model* header = nil;
    BOOL isHeader = FALSE;
    for (int i = 0; i < control.child_count; i++){
        list_item = control.children[i];
        
        // count num columns from header, or from last row (if header not found)
        if (!isHeader &&
            ((isHeader = [list_item.type caseInsensitiveCompare:@"header"] == NSOrderedSame) ||
             (list_item == [control.children lastObject]))) {
                if (isHeader) { // remove header from listitem
                    header = list_item;
                    i = (int)[self removeModel:list_item];
                }
                else { // use the last list-item as header
                    header = [list_item copy];
                    [self filterChildrenIn:header validTypes: @[@"edit"]];
                    header.width = control.width - 2 ;
                }
            }
    }
    return header;
}

- (NSTableView*) drawList:(Model*) model frame:(NSRect)frame parentView:(NSView *) parent {
    //filter unwanted element
    [self filterChildrenIn:model validTypes: @[@"listitem", @"header"]];
    // grab the header
    Model * header = [self getListHeader:model];
    
    CustomTableView* tableView; //    //NSTableView* tableView;
    if ((tableView  = [idToUITable objectForKey:model.unique_id])) {
        //update the column
        [tableView updateWithRoot:model andHeader:header];
        //[tableView reloadData];
        // reset the version
        model.version = 0;
        //NSAccessibilityPostNotification(tableView, NSAccessibilityRowCountChangedNotification);
        NSLog(@"[sinter] - :%.0f: list_update", CACurrentMediaTime()*1000000000);
        return tableView;
    }
    // create a new instance
    tableView = [[CustomTableView alloc] initWithFrame:frame Model:model header:header andContainer:parent];
    
    //store references
    if (![idToUITable objectForKey:tableView.identifier]) {
        [idToUITable setObject:tableView forKey:tableView.identifier];
    }
    
    return tableView;
}


#pragma mark OutlineView/Tree BEGIN
- (NSOutlineView *) drawTree:(Model*) control frame:(NSRect)frame anchor:(id) anchor{
    //filter unwanter node(s)
    [self filterChildrenIn:control validTypes:@[@"treeitem"]];
    //control = [self filterTreeItem:control];

    //NSOutlineView * outlineView;
    CustomOutlineView * outlineView;
    if ((outlineView  = [idToUITable objectForKey:control.unique_id])) {
        @try {
            [outlineView endUpdates];
            [outlineView beginUpdates];
            [outlineView numberOfChildrenOfItem:anchor];
            //[self outlineView:outlineView isItemExpandable:anchor];
            
            [outlineView reloadItem:anchor reloadChildren:YES];
            //[outlineView reloadItem:anchor];
            //[outlineView expandItem:anchor];
            [outlineView endUpdates];
            NSLog(@"[sinter] - :%.0f: tree_update", CACurrentMediaTime()*1000000000);
        }
        @catch (NSException *exception) {
            NSLog(@"exception happened during refreshing the tree %@", [anchor name]);
        }
        
        if (control.version) {
            control.version = 0;
        }
        return outlineView;
    }
    outlineView = [[CustomOutlineView alloc] initWithFrame:frame model:control andContainer:anchor];

    //store references
    if (![idToUITable objectForKey:outlineView.identifier]) {
        [idToUITable setObject:outlineView forKey:outlineView.identifier];
    }

    return outlineView;
}

#pragma mark Menu, MenuBar
- (void) drawMenuBar:(Model*) model parentView:(NSMenu *) menu {
    if ([model.name hasPrefix:@"System"])
        return;
    
    if ([idToUITable objectForKey:menu.title]) {
        Model* selectedMenu = selectedMenuModel;
        selectedMenu.version = 1;
        [self menuNeedsUpdate:menu];
        return;
    }
    
    // note: the root-menu's identifier is its 'title'
    [menu setTitle:model.unique_id];
    [idToUITable setObject:menu forKey:menu.title];
    
    // now add menuItems
    NSMenuItem *menuBarItem = nil;
    
    // 1st menuItem is always a dummy
    menuBarItem = [[NSMenuItem alloc] initWithTitle:@"Application" action:nil keyEquivalent:@""];
    [menu addItem:menuBarItem];
    
    // next add the actual menuItems
    menuBarItem = nil;
    for (int i = 0 ; i < model.child_count; i++) {
        [self populateMenu:menu usingModel:model atIndex:i andCreateSubMenu:YES];
    }
    
    //NSMenuDidEndTrackingNotification
    //[[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(menuWillSendAction:) name:NSMenuDidEndTrackingNotification object:nil];

}

- (Model *) findSubMenuModelInParentModel:(Model *) parentModel {
    Model * model = parentModel;
    while ([model.type caseInsensitiveCompare:@"menu"] != NSOrderedSame) {
        if (([model.type caseInsensitiveCompare:@"menuitem"] == NSOrderedSame && model.child_count > 0)
            || (([model.type caseInsensitiveCompare:@"menubaritem"] == NSOrderedSame && model.child_count > 0))){
            model = [model.children firstObject];
        } else { // unknow model-type
            model = nil;
			//break; 
        }
		break; //this bug results no menu rendered! 
    }
    return model;
}

- (Model *) getModelForMenu:(id) menuOrMenuItem{
    NSMenu* menu;
    NSMenuItem* item;
    
    if ([menuOrMenuItem isKindOfClass:[NSMenuItem class]]) {
        item = menuOrMenuItem;
    }
    if ([menuOrMenuItem isKindOfClass:[NSMenu class]]) {
        menu = menuOrMenuItem;
        NSMenu* parent_menu = menu;
        if ([parent_menu supermenu]) {
            parent_menu = [parent_menu supermenu];
        }
        
        int index = (int)[parent_menu indexOfItemWithSubmenu:menu];
        item = [parent_menu itemAtIndex:index];
    }
    if (!item) {
        return nil;
    }
    
    Model* model = [idTable objectForKey: [item representedObject]];
    return model;
}

- (void) populateMenu:(NSMenu*) menu usingModel:(Model *) model atIndex:(NSInteger) index andCreateSubMenu:(BOOL) _createSubMenu{
    NSMutableArray * items = [[NSMutableArray alloc] init];
    if (index == ALL) {// add all
        items = model.children;
        [menu removeAllItems];
    } else {
        [items addObject:[model.children objectAtIndex:index]];
    }
    
    NSMenuItem *subMenuItem;
    for (int i = 0 ; i < items.count ; i++) {
        Model* child = items[i];
        if([child.type caseInsensitiveCompare:@"separator"] != NSOrderedSame){
            subMenuItem = [[NSMenuItem alloc] initWithTitle:child.name action:@selector(menuAction:)  keyEquivalent:@"" ];
            [subMenuItem setTarget:self];
            if (child.states & STATE_DISABLED) {
                [subMenuItem setEnabled:NO];
            }
            //handling shortcuts
            if (![child.value isEqualToString:@""]) {
                [subMenuItem setTitle:[NSString stringWithFormat:@"%@ (%@)", child.name, child.value]];
            }
            /* //disabling it for now
            NSArray* parts = [child.value componentsSeparatedByString:@"+"];
            if (parts) {
                [subMenuItem setKeyEquivalent:[parts lastObject]];
                if ([parts count] > 1) {
                    NSString* modifier;
                    for (int i=0; i< [parts count] - 1 ; i++) {
                        modifier = parts[i];
                        if ([modifier isEqualToString:@"Crtl"]) {
                            [subMenuItem setKeyEquivalentModifierMask:NSControlKeyMask];
                        }
                    }
                }
            }*/
            
            // primary-key  mapping
            [subMenuItem setRepresentedObject:child.unique_id];
            [idToUITable setObject:subMenuItem forKey:child.unique_id];
        } else { // separator
            subMenuItem = [NSMenuItem separatorItem];
        }
        [menu addItem: subMenuItem];
        
        if (_createSubMenu || (child.states & STATE_COLLAPSED)) {
            // add sub-menu to it's containing menuBarItem
            NSMenu *submenu = [[NSMenu alloc] initWithTitle:child.name];
            NSMenuItem * submenItem = [[NSMenuItem alloc] initWithTitle:
                                       [NSString stringWithFormat:@"Loading %@ Menu, please try again",submenu.title]
                                                                 action:nil keyEquivalent:@""];
            [submenItem setAlternate:YES];
            [submenu addItem:submenItem];
            [subMenuItem setSubmenu:submenu];
            // add sub-menu delegate
            [submenu setDelegate:(id<NSMenuDelegate>)self];
        }
    }
}

- (void) drawMenu:(Model*) model anchor:(id) anchor {
    // sanity check
    if (![anchor isKindOfClass:[NSMenu class]])
        return;
    NSMenu* menu = (NSMenu*) anchor;
    /*
    if (model != selectedMenuModel) {
        NSLog(@"current menu %@, local menu %@", [model name] , [selectedMenuModel name]);
        selectedMenuModel = model;
        return;
    }*/

    NSMenuItem* menuItem = [idToUITable objectForKey:model.unique_id];
    // check if it is a submenu
    if (menuItem && menuItem.hasSubmenu) {
        menu = [menuItem submenu];
    }
    //NSLog(@"updated menu %@", menu.title);
    [self menuNeedsUpdate:menu];
}

- (void) menuNeedsUpdate:(NSMenu*)menu {
    Model* model = selectedMenuModel;
    // prevent auto-call to this method
    if (!model || !model.version) {
        return;
    }

    Model* modelSubMenu = [self findSubMenuModelInParentModel:model];
    if (modelSubMenu) {
        [self populateMenu:menu usingModel:modelSubMenu atIndex:ALL andCreateSubMenu:NO];
        if (model.version) {
            model.version = 0;
        }
    }
}

- (void) menu:(NSMenu *)menu willHighlightItem:(NSMenuItem *)item {
    Model* model = [idTable objectForKey:[item representedObject]];
    if (!model) return;
    // send foucs
    //hack: if it has checked state, then send
//    if (model.states & STATE_CHECKED) {
//        [sharedConnection sendMouseMoveAt:model.unique_id andX:model.left andY:model.top];
//    } else {
        [sharedConnection sendActionMsg:process_id targetId:(model.unique_id) actionType:STRActionChangeFocus data:nil];
//    }
    
}

- (void) menuWillOpen:(NSMenu *) menu {
    Model* model = [self getModelForMenu:menu];
    //NSLog(@"in willopen current menu %@, local menu %@", [model name] , [selectedMenuModel name]);
    if (model && fabs([lasOperationTime timeIntervalSinceNow]) > 0.2 ) {
        [sharedConnection sendActionMsg:process_id targetId:(model.unique_id) actionType:STRActionExpand data:nil];
        selectedMenuModel = model;
        lasOperationTime = [NSDate date];
        
        //NSLog(@"opened %@", model.name);
    }
}
/*
- (void) menuDidClose:(NSMenu *)menu {
    Model* model = [self getModelForMenu:menu];
    if (model && model != selectedMenuModel){
        //[sharedConnection sendActionAt:model.unique_id actionName:@"collapse"];
        //selected = nil;
    }
}
*/

- (void) menuAction: (id) sender{
    NSMenuItem *item = sender;
    Model * model = [idTable objectForKey:[item representedObject]];
    if (model) {
        [sharedConnection sendActionMsg:process_id targetId:(model.unique_id) actionType:STRActionDefault data:nil];
        selectedMenuModel = nil;
    }
}


#pragma mark action handler
- (void) sendFocus:(NSView*) sender {
    if (sender.identifier) {
        [sharedConnection sendActionMsg:process_id targetId:sender.identifier actionType:STRActionChangeFocus data:nil];
    }
}

- (void) sendFocusToSynchronize:(NSView*) sender{
    if (sender.identifier) {
        Model* item = [idTable objectForKey:sender.identifier];
        if (item) {
            [sharedConnection sendActionMsg:process_id targetId:(sender.identifier) actionType:STRActionChangeFocus data:item.name];
        }
    }
}

- (IBAction) sendAction:(id) sender {
    NSString* unique_id  = [(NSControl *)sender identifier];
    if (unique_id ) {
        Model *uiRemote = [idTable objectForKey:unique_id];
        if (uiRemote) {
            [sharedConnection sendActionMsg:process_id targetId:unique_id actionType:STRActionDefault data:nil];
        }
    }
}

@end
