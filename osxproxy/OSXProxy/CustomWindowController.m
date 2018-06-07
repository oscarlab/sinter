//
//  RemoteWindowController.m
//  RemoteProcess
//
//  Created by Syed Masum Billah on 7/21/14.
//
//

#import "CustomWindowController.h"
#import "Model.h"
#import "ClientHandler.h"
#import "KeyMapping.h"
#import "ControlTypes.h"

#import "XMLTags.h"


#import "MegaWordRibbon.h"

@implementation CustomWindowController

@synthesize xmlDOM;
@synthesize process_id;
@synthesize shouldClose;
@synthesize localWinFrame;

@synthesize rmUiRoot;
@synthesize isChild;
@synthesize keystrokes;
@synthesize remoteProcess;
@synthesize focussedView;
@synthesize prev_selected_menu_index, prev_menu_x,prev_menu_y,prev_action_x,prev_action_y;
@synthesize focussedID;
@synthesize service_codes;

@synthesize idToUITable;
@synthesize idTable;
@synthesize screenMapTable;
@synthesize sharedConnection;

int dragButtonTag = 9999999;
MegaWordRibbon *customRibbon;

int defaultfontSize = 10;
int scalingFactor = 1;//2; - -for ipad scaling
int min_Button_height = 50;

int fontSize;
int L, mask;

//CFTimeInterval startTime, elapsedTime;

- (void) awakeFromNib {
    fontSize = defaultfontSize * scalingFactor;
}

- (BOOL)windowShouldClose:(id)sender {
    // closing a window is a complex process:
    // first send a 'close' event to remote window.
    // if it's closed, then it returns a close-ack in appDelegate
    // AppDelegate sets 'ShoudlClose' to YES.
    if(![self shouldClose] && [sharedConnection isConnected]){
        [sharedConnection sendActionAt:rmUiRoot.unique_id actionName:@"close"];
        return NO;
    }
    [screenMapTable removeAllObjects];
    [idToUITable removeAllObjects];
    [idTable removeAllObjects];
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    

    //NSLog(@"Window destroyed");

    return YES;
}

- (void) windowDidResignKey:(NSNotification *) notification {
    [[NSApplication sharedApplication] setMainMenu:mainMenu];
    //NSLog(@"notification name = %@", [notification name]);
}

- (void)windowDidBecomeKey:(NSNotification *) notification {
    [[NSApplication sharedApplication] setMainMenu:remoteMenu];
    
    //bring this application to foreground remotely
    [sharedConnection sendBtingFG:process_id];
}


- (void) setUpConnectionAndListener{
    sharedConnection = [ClientHandler getConnection];
    
    //register callback
    service_codes = [[NSArray alloc] initWithObjects:[NSString stringWithFormat:@"%i", SERVICE_CODE_DELTA], nil];
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(takeActionForXML:) name:process_id object:nil];
    
    //[sharedConnection registerListener:self withPID:rmUiRoot.unique_id forServiceCodes:service_codes];
    
    CustomWindow* window = (CustomWindow*)[self window];
    [window setSharedConnection:sharedConnection];
}

- (Model *) populateValuesFrom:(Entity *) node addToCache:(BOOL) _addToCache updateType: (NSString*) updateType{
    NSString* unique_id = node.unique_id;
    
    Model * ui = [idTable objectForKey:unique_id];
    if (ui) {// already exists
        ui.version = UPDATED;
        //NSLog(@"id exists %@", unique_id);
    }
    else {
        // new item
        ui = [[Model alloc] init];
        if (_addToCache) {
            [idTable setObject:ui forKey:unique_id];
        }
        ui.unique_id        = unique_id;
        ui.version          = STALE_OR_NEW;
    }
    
    if ([updateType isEqualToString: updateTypeNameChanged]){
        ui.name             = node.name; // [attrs objectForKey:nameTag];
        ui.states           = [node.states longLongValue];
    }
    else if ([updateType isEqualToString: updateTypeValueChanged]){
        ui.value            = node.value;
        ui.states           = [node.states longLongValue];
    }
    else {
        // populate data
        ui.type             = node.type;
        ui.width            = [node.width intValue];
        ui.height           = [node.height intValue];
        
        ui.top              = [node.top intValue];
        ui.left             = ui.left = [node.left intValue];
        //ui.child_count      = [[attrs objectForKey:childCountTag] intValue];
        ui.name             = node.name;
        ui.value            = node.value;
        ui.states           = [node.states longLongValue];
    }
    
    // sanity check
    ui.left    = ui.left   < 0 ? 0 : ui.left;
    ui.top     = ui.top    < 0 ? 0 : ui.top;
    ui.width   = ui.width  < 0 ? 0 : ui.width;
    ui.height  = ui.height < 0 ? 0 : ui.height;
    
    return ui;
}

- (Model*) parseXMLDocument:(Entity *) xmlNode havingUI:(Model *) ui anchor:(NSString *) anchorId updateType:(NSString*) updateType {
    if (!anchorId) return nil;
    Model* anchor = [idTable objectForKey:anchorId];
    if (!anchor) return nil;
    
    if (!anchor.version) {
        [self parseXMLDocument:xmlNode havingUI:anchor updateType:updateType];
        anchor.version++;
    }
    return anchor;
}

- (Model*) parseXMLDocument:(Entity *) xmlNode havingUI:(Model *) ui updateType:(NSString*) updateType {
    NSArray* children = xmlNode.children;
    if(!children) return ui;
    if (![children count]) return ui;
    
    if(!ui.children){
        ui.children = [[NSMutableArray alloc] init];
    }
    else {
        // remote old instances
        if ([updateType isEqualToString:updateTypeChildUpdated]) {
            for (Model* old in ui.children) {
                old.states |= STATE_INVISIBLE;
            }
        }
        else if ([updateType isEqualToString:updateTypeChildAdded]) {
            ui.child_count += (int)[ui.children count];
        }
        else if ([updateType isEqualToString:updateTypeNodeExpanded]) {
            //do not delete old data, just update;
        }
        else { // default action
            for (Model* old in ui.children) {
                [idTable removeObjectForKey:old.unique_id];
            }
            [ui.children removeAllObjects];
        }
    }
    
    for (Entity * child_xml in children) {
        Model* child_ui = [self populateValuesFrom:child_xml addToCache:YES updateType:updateType];
        
        if (child_ui && child_ui.version == UPDATED) {
            // already exists, and updated by populateValues method.
            if (![ui.children containsObject:child_ui]) {
                [ui.children addObject:child_ui];                
            }
        }
        else {
            [ui.children addObject:child_ui];
            child_ui.parent = ui; // add parent
            child_ui.version = UPDATED; // temporary
        }
        
        // recursive call
        [self parseXMLDocument:child_xml  havingUI:child_ui updateType:updateType];
    }
    
    // clean-up the old nodes
    for (int i = 0 ; i < [ui.children count]; i++) {
        Model* child_ui = ui.children[i];
        if (child_ui.version != UPDATED) {
            [ui.children removeObject:child_ui];
            i--;
        } else {
            child_ui.version = STALE_OR_NEW;
        }
    }
    
    //now update the child_count field
    ui.child_count = ui.children ? (int)[ui.children count]: 0 ;
    return ui;
}

- (void) setUpWindowFramAndDOM: (Entity *) root {
    if(!root){
        NSLog(@"AppRoot not found, xml parsing error. Exiting...");
        return;
    }
    rmUiRoot = [self populateValuesFrom:root addToCache:YES updateType:nil];
    
    rmUiRoot.parent = nil;
    [self parseXMLDocument:root havingUI:rmUiRoot updateType:nil];
    
    /*
    // aspect ratio
    rm_screen_h = [[XMLParser getHeaderAttributeValueFor:
                                     DOM andAttribute:@"screen_height"] intValue];
    rm_screen_w = [[XMLParser getHeaderAttributeValueFor:
                                    DOM andAttribute:@"screen_width"] intValue];

    if (rmUiRoot.width && rmUiRoot.height && rm_screen_h && rm_screen_w) {
        _rm_screen_ratio_x = rmUiRoot.width/rm_screen_w;
        _rm_screen_ratio_y = rmUiRoot.height/rm_screen_h;
    }else{
        _rm_screen_ratio_x = 1.0;
        _rm_screen_ratio_y = 1.0;
    }
     */
    //setting window title
    if ([rmUiRoot.name length])
        [self.window setTitle:rmUiRoot.name];
    else
        [self.window setTitle:@"Untitled"];
    
    NSRect frame = [self.window frame];
    frame.size.height = rmUiRoot.height;
    frame.size.width = rmUiRoot.width;
    
    //set window frame
    [self.window setFrame:frame display:TRUE ];
    localWinFrame =[self.window frame];
    
    //NSLog(@"remote window frame (x%i y%i w%i h%i)\n",rmUiRoot.left,rmUiRoot.top, rmUiRoot.width, rmUiRoot.height);
    //NSLog(@"local window frame (x%f y%f w%f h%f)", localWinFrame.origin.x, localWinFrame.origin.y, localWinFrame.size.width, localWinFrame.size.height);
    
    //create menu
    mainMenu = [[NSApplication sharedApplication] mainMenu];
    remoteMenu = [[NSMenu alloc] initWithTitle:@"Custom"];

    [self.window autorecalculatesKeyViewLoop];
    [self.window setAutorecalculatesKeyViewLoop:YES];

}

// MARK: Window Init
- (id) initWithWindowNibName:(NSString *)windowNibName fromEntity:(Entity *) entity havingProcessID:(NSString*) processId {
    CustomWindow* window = [[CustomWindow alloc] init];
    self = [[CustomWindowController alloc] initWithWindow:window];
    
    if(self){
        [self setXmlDOM:entity];
        [self setProcess_id:processId];
        [self setShouldClose:NO];
        
        // do the rendering in the background
        dispatch_async(dispatch_get_main_queue(), ^{ //
        //dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_HIGH, 0), ^{ // 1

            // init window frame, title, and parse DOM
            [self setUpWindowFramAndDOM: entity];
            [self setUpConnectionAndListener];
            
            // render DOM
            renderer = [[DrawUI alloc] initWithProcessID:process_id
                                              andIDTable:idTable
                                             idToUITable:idToUITable
                                           screenMapTable: screenMapTable
                                      havingRemoteRootUI:rmUiRoot
                                               andWindow:[self window]];
            [self renderDOM:rmUiRoot anchor:self.window.contentView];

            // callback for show window
            //dispatch_async(dispatch_get_main_queue(), ^{ // 2
                //redraw
                [[self.window contentView] setNeedsDisplay:YES];
                [self showWindow:nil];
                
                //[self.window.menu setDelegate:self];
                //[remoteMenu setMenuChangedMessagesEnabled:TRUE];
                [[NSApplication sharedApplication] setMainMenu:remoteMenu];
                //NSLog(@"finish initial rendering:");
                
                //time-log
                NSLog(@"[sinter] - :%.0f: window_end", CACurrentMediaTime()*1000000000);
                //startTime = CACurrentMediaTime();

            //});
        });
        return self;
    }
    return nil;
}

- (id) initWithWindow:(NSWindow *)window {
    self = [super initWithWindow:window];
    if (self) {
        if(!idTable){
            idTable = [[NSMutableDictionary alloc] init];
        }
        if (!idToUITable) {
            idToUITable = [[NSMutableDictionary alloc] init];
        }
        if (!screenMapTable) {
            screenMapTable = [[NSMutableDictionary alloc] init];
        }
        if(!customToolbarTable){
            customToolbarTable = [[NSMutableDictionary alloc] init];
        }
        [window setDelegate:self];
        charsetLetter = [NSCharacterSet letterCharacterSet];
        charsetNumber = [NSCharacterSet alphanumericCharacterSet];
        charsetCustom = [[NSArray alloc] initWithObjects: @(0x1B),
                         @(NSCarriageReturnCharacter),
                         @(NSBackspaceCharacter),
                         @(NSDeleteCharacter), nil];
        keyPressCount = 1;
        
    }
    return self;
}

- (void) windowDidLoad {
    [super windowDidLoad];
    [self.window setDelegate:self];    
}


NSView *lastView = nil;
bool enableDrag = false;
- (void) moveit:(NSEvent*)click {
    int x_new;
    int y_new;
    
    if ([lastView superview] == self.window.contentView)
    {
        x_new = click.locationInWindow.x;
        y_new = click.locationInWindow.y;
    }
    else
    {
        //        CGPoint xy1 = lastView.frame.origin;
        //        CGPoint xy2 = [lastView superview].frame.origin;
        //        CGPoint xy3 = [[lastView superview] superview].frame.origin;
        CGPoint xy4 = [[[lastView superview] superview] superview].frame.origin;
        // NSView *v = [[lastView superview] superview];
        x_new = click.locationInWindow.x - xy4.x -20;
        y_new = click.locationInWindow.y - xy4.y -25;
    }
    
    NSRect fr =   NSMakeRect(x_new,
                             y_new,
                             lastView.frame.size.width,
                             lastView.frame.size.height);
    
    [lastView setFrame:fr];
    [lastView setNeedsDisplay:TRUE];
    lastView = nil;

}

// MARK: virtual keyboard, mouse handling
- (void) mouseDown:(NSEvent*)click {
    int x1 = 0,y1=0;
    
    NSView *deepView = [[[self window] contentView] hitTest:[click locationInWindow]];
    //NSLog(@"%@",deepView);
    //NSLog(@"%@",[deepView className]);
    
    if (enableDrag)
    {
        if ([deepView tag] == dragButtonTag)
        {
            NSButton *dragButton = (NSButton *) deepView;
            [dragButton setTitle:enableDrag ? @"EnableDragging" : @"DisableDragging"];
            [dragButton setNeedsDisplay:TRUE];
            enableDrag = enableDrag ? false : true;
        }
        else
        {
            if (lastView)
            {
                [self moveit:click];
                lastView = nil;
            }
            else
            {
                if ([[deepView className] isEqualToString:@"NSButton"] ||
                    [[deepView className] isEqualToString:@"NSComboBox"] ||
                    [[deepView className] isEqualToString:@"NSPopUpButton"])
                {
                    lastView = deepView;
                }
            }
        }
        return;
    }
    
    if ([[deepView className] isEqualToString:@"NSOutlineView"]) {
        NSOutlineView *outView = (NSOutlineView *) deepView;
        //x1 =[[outView itemAtRow:[outView selectedRow]] left];
        //y1 = [[outView itemAtRow:[outView selectedRow]] top];
        //NSLog(@"%i %i",x1,y1);
        focussedView = outView;
        return;
    }
    else  if ([[deepView className] isEqualToString:@"NSTableView"]) {
        focussedView = (NSTableView *) deepView;
        return;
    }
    else  if ([[deepView className] isEqualToString:@"NSButton"])
    {
        if ([deepView tag] == dragButtonTag)
        {
            NSButton *dragButton = (NSButton *) deepView;
            [dragButton setTitle:enableDrag ? @"EnableDragging" : @"DisableDragging"];
            [dragButton setNeedsDisplay:TRUE];
            enableDrag = enableDrag ? false : true;
        }
        //        [self.window makeFirstResponder:(NSView *)[idToUITable objectForKey:focussedID]];
        return;
    }
    else  if ([[deepView className] isEqualToString:@"NSTextField"]) {
        NSTextField *txtFld = (NSTextField *) deepView;
        x1 =([txtFld selectedTag]&mask) >> L;
        y1 = ([txtFld selectedTag]& ~mask);
        //NSLog(@"%i %i",x1,y1);
    }
    else {
        
        //        x1 = (int)(([click locationInWindow].x) *(remoteProcess.width/hostWindowPos.size.width) + remoteProcess.left);
        //        y1 = (int)((hostWindowPos.size.height-([click locationInWindow].y))*(remoteProcess.height/hostWindowPos.size.height) + remoteProcess.top)
        x1 =([deepView tag]&mask) >> L;
        y1 = ([deepView tag]& ~mask);
        
    }
    
//    if (sharedConnection.isConnected) {
//        if([deepView tag] != -1)
//            [sharedConnection sendMessageWithData:[XMLSerializer serializeCommandForMouseWith:process_id andX:x1 andY:y1 andButton:1]];
//        else
//            [sharedConnection sendMessageWithData:[XMLSerializer serializeCommandForFGWith:process_id]];
//    }
}

- (void) moveDown:(id)sender {
//    [self scrollLineDown:sender];
}

- (void) moveUp:(id)sender {
//    [self scrollLineUp:sender];
}


int xCoord = 0;
int yCoord = 120;
int toolBarCount = 0;
NSMutableArray *lru_customToolbar;
int maxMegaRibbonSize = 10;


- (void)rightMouseDown:(NSEvent *)theEvent{
    //NSLog(@"Right Mouse down");
    NSMenu *theMenu1 = [[NSMenu alloc] initWithTitle:@"Contextual Menu"];
    [theMenu1 insertItemWithTitle:@"Option1" action:nil keyEquivalent:@"" atIndex:0];
    [theMenu1 insertItemWithTitle:@"Option2" action:nil keyEquivalent:@"" atIndex:1];
    [theMenu1 insertItemWithTitle:@"Option3" action:nil keyEquivalent:@"" atIndex:1];
    [theMenu1 insertItemWithTitle:@"Option4" action:nil keyEquivalent:@"" atIndex:1];
    
    [NSMenu popUpContextMenu:theMenu1 withEvent:theEvent forView:self.window.contentView];
}

int temp_index = 0;
bool isHeader = FALSE;

- (NSRect) getHostFrameForRemoteUI:(Model*)child {
    NSRect uiFrame = localWinFrame;
    uiFrame.origin.x = (child.left - rmUiRoot.left); //offset
    uiFrame.origin.y = localWinFrame.size.height -  (child.height+ (child.top - rmUiRoot.top));
    uiFrame.size.height = child.height;
    uiFrame.size.width = child.width;
    
    return uiFrame;
}

- (NSRect) getHostFrameForTab:(Model*)child frame:(NSRect) child_frame{
    NSRect uiFrame = [tabView frame];
    uiFrame.origin.x =   (child_frame.origin.x - uiFrame.origin.x);
    uiFrame.origin.y = child_frame.origin.y - uiFrame.origin.y;
    //    uiFrame.origin.y =   (uiFrame.size.height ) - (child.height + child.top) + 12;
    uiFrame.size.height = child.height;
    uiFrame.size.width = child.width;
    
    return uiFrame;
}

-(void) drawCustomRibbon {
    customRibbon = [[MegaWordRibbon alloc] initWithWindowNibName:@"MegaWordRibbon"];
    [customRibbon.window autorecalculatesKeyViewLoop];
    [customRibbon.window setAutorecalculatesKeyViewLoop:YES];
    [[customRibbon window]  setLevel:NSFloatingWindowLevel];
    [[customRibbon.window contentView] setNeedsDisplay:YES];
    [customRibbon showWindow:self];
}

- (void) printElapsedTimeWith:(NSString*) msg{
    //elapsedTime = CACurrentMediaTime();
    //NSLog(@"%@ render time: %f", msg, (elapsedTime - startTime));
}

#pragma mark KEYBOARD events
-(void)keyDown:(NSEvent *)theEvent {
//    if (theEvent.isARepeat) {
//        keyPressCount++;
//    }
    [self interpretKeyEvents:[NSArray arrayWithObject:theEvent]];

}

// keyDown handler variables
//- (void) keyUp:(NSEvent *)theEvent{
//    [self interpretKeyEvents:[NSArray arrayWithObject:theEvent]];
//    keyPressCount = 1;
//}

//- (void) keyUp2:(NSEvent *)theEvent{
//    // there are 2 cases:
//    // 1. if 'Crtl' + 'anyKey' is pressed, we'll allow sending keys
//    // 2. if Enter, Backspace, Escape pressed, we'll allows sending keys
//    // check 'sendKeys' format from msdn
//    
//    shouldSendKey = !restrictKeystoke;
//    //case 1
//    flags = [NSEvent modifierFlags]; //
//    if( flags & NSControlKeyMask ){//CONTROL
//        shouldSendKey = YES;
//    }
//    // case 2
//    _key = [theEvent charactersIgnoringModifiers];
//    if ([_key length] &&
//        [charsetCustom containsObject:@([_key characterAtIndex:0])]) {
//        shouldSendKey = YES;
//    }
//    if (!shouldSendKey){
//        keyPressCount = 1;
//        return;
//    }
//    
//    //key processing
//    key = [KeyMapping keyStringFormKeyCode:theEvent.keyCode];
//    if (!key) {
//        key = _key;
//        // add a place holder to current key for repetition (i.e. 'a' to become {a%@})
//        key = [NSString stringWithFormat:@"{%@%@}", key,
//               [key characterAtIndex:0] =='%' ? @"%%@" : @"%@"];
//    }
//    
//    // handling modifiers, e.g., ALT, CAPS, CRTL, etc.
//    modifier = @"%@";
//    if( (flags & NSShiftKeyMask) && (flags & NSAlphaShiftKeyMask))//SHIFT CAPS
//        modifier = @"+({CAPSLOCK}%@)";
//    else if( flags & NSShiftKeyMask )//SHIFT +
//        modifier = @"+(%@)";
//    else if( flags & NSAlphaShiftKeyMask )//CAPS
//        modifier = @"{CAPSLOCK}%@";
//    else if( flags & NSControlKeyMask )//CONTROL ^
//        modifier = @"^(%@)";
//    else if( flags & NSAlternateKeyMask )//ALT %
//        modifier = @"%(%@)";
//    else {
//        // nothing
//    }
//    // converting keys, i.e. Crtl+a becomes @^({a%@})
//    key = [NSString stringWithFormat:modifier, key];
//    //adding key repeat count, i.e. Crtl+a becomes @^({a%@}) = @^({a 1})
//    key = [NSString stringWithFormat:key, [NSString stringWithFormat:@" %i", keyPressCount]];
//    [sharedConnection sendKeystorkesAt:process_id strokes:key];
//    
//    // reset key log
//    keystrokes = @"";
//    keyPressCount = 1;
//    
//    //time-log
//    startTime = CACurrentMediaTime();
//}
//
#pragma mark NSResponder

- (void) cancelOperation:(id)sender{
    [sharedConnection sendSpecialStroke:@"ESC" numRepeat:keyPressCount];
}

- (void)insertText:(id)insertString {
    if(![[[self window] firstResponder] respondsToSelector:NSSelectorFromString(@"shouldSendKeyStrokes")]){
        [sharedConnection sendSpecialStroke:insertString numRepeat:keyPressCount];
        NSLog(@"cool");
    }
}


#pragma mark Render all ui components
- (NSView*) renderDOM:(Model *) current anchor:(id) anchor{
    //NSView* ui = (NSView *)[[NSClassFromString(@"NSButton") alloc] init];
    //[ui setFrame:frame];
    
    NSRect uiFrame = [renderer getLocalFrameForRemoteFrame:current];
    NSView* current_view = nil;
    if ([current.type isEqualToString:@"list"]) {
        current_view = [renderer drawList:current frame:uiFrame parentView:anchor];
        [self printElapsedTimeWith:@"list"];
    }
    else if ([current.type isEqualToString:@"tree"]) {
       dispatch_async(dispatch_get_main_queue(), ^{ // 2
        [renderer drawTree:current frame:uiFrame anchor:anchor];
        [self printElapsedTimeWith:@"tree"];
       });
    }
    else if ([current.type isEqualToString:@"treeitem"]) { // during update treeitem
        Model* anchor = [DrawUI getParentOf:current havingRole:@"tree"];
        if (anchor) {
            current_view = [self renderDOM:anchor anchor:current];
        }
    }
    else if ([current.type isEqualToString:@"button"]) {
        current_view = [renderer drawButton:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type isEqualToString:@"radiobutton"]) {
        current_view = [renderer drawRadioButton:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type isEqualToString:@"checkbox"]) {
        current_view = [renderer drawCheckBox:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type hasPrefix:@"text"]) {
        current_view = [renderer drawText:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type hasPrefix:@"edit"]) {
        current_view = [renderer drawSimpleEditText:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type isEqualToString:@"document"]) {
        current_view = [renderer drawEditText:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type isEqualToString:@"menubar"]) {
        [renderer drawMenuBar:current parentView:remoteMenu];
    }
    else if ([current.type isEqualToString:@"menuitem"]) {
        [renderer drawMenu:current anchor:remoteMenu];
    }
    else if ([current.type isEqualToString:@"searchbox"]) {
        [renderer drawSearchField:current frame:uiFrame parentView:anchor];
        [self printElapsedTimeWith:@"searchbox"];
    }
    else if ([current.type isEqualToString:@"titlebar"]) {//ignore system buttons
        [self.window setTitle:current.name];
    }
    else if ([current.type isEqualToString:@"combobox"]) {
        current_view = [renderer drawComboBox:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type hasPrefix:@"breadcrumb"]) {//composite ui
        current_view = [renderer drawBreadCrumb:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type isEqualToString:@"toolbar"]) {//composite
        current_view = [renderer drawToolbar:current frame:uiFrame parentView:anchor];
    }
    else if ([current.type hasPrefix:@"uiribbon"]) {//ribbon, ignore
        //ignore 
    }
    else if ([current.type hasPrefix:@"scrollbar"]) {//scrollbar, ignore
        //ignore
    }
    else if ([current.type hasPrefix:@"group"]) {//scrollbar, ignore
        [renderer drawGroup:current frame:uiFrame];
    }
    else if ([current.type isEqualToString:@"progressbar"]) {//composite, complex
        [renderer drawProgressBar:current frame:uiFrame parentView:anchor];
        // go over each child, i.e., breadcrumb, toolbar, combobox
        for (int i=0; i< current.child_count; i++) {
            Model* ui =  current.children[i];
            [self renderDOM:ui anchor:anchor];
        }
    }
    else { // go down recursively for children
        for (Model* child in current.children) {
            [self renderDOM:child anchor:anchor];
        }
    }
    // set the current focus
    
    if (current.states & STATE_FOCUSED) {
        renderer.focusedModel = current;
        //[[self window] makeFirstResponder:current_view];
        //current.states ^= STATE_FOCUSED;
    }
    return current_view;
}


// MARK: consume updates
- (void) takeActionForXML:(Sinter *) xmlDoc {
    //NSLog(@" receive-at %f", CACurrentMediaTime());
    //dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_BACKGROUND, 0), ^{ // 1

    if([xmlDoc.header.service_code integerValue] != SERVICE_CODE_DELTA) {
        return;
    }

    NSString *anchorId, *updateType;
    if(xmlDoc.header.kbd_or_action) {
        anchorId = xmlDoc.header.kbd_or_action.target_id;
        updateType = xmlDoc.header.kbd_or_action.generic_data;
    }

    // now read the next nodes
    Model* ui;
    NSArray * nodes = xmlDoc.updates;
    Entity * node;
    for (int i = 1 ; i < [nodes count] ; i++ ){
        node = nodes[i];
        ui = nil;

        // if focused-changed, then do very less
        if ([updateType isEqualToString:updateTypeFocusChanged]) {
            ui = [self populateValuesFrom:node addToCache:NO updateType:updateType];
            if (ui && ui.version && [ui.type hasPrefix:@"text"]) {
                [self renderDOM:ui anchor:self.window.contentView];
                ui.version = 0;
            }
            continue;
        }
        // otherwise, do regular update operation
        // parse the root
        ui = [self populateValuesFrom:node addToCache:YES updateType:updateType];
        // see if it already in our cache
        if (ui && ui.version) {
            //NSLog(@"found an anchor, attaching...%@", ui.name);
            [self parseXMLDocument:node havingUI:ui updateType:updateType];
            if ([ui.type hasPrefix:@"menu"]) {
                [self renderDOM:ui anchor:remoteMenu];
            } else {
                [self renderDOM:ui anchor:self.window.contentView];
            }
        }
        else { //NSEventTrackingRunLoopMode
            //NSLog(@"didn't find an anchor, attaching %@ to %@...", ui.name, anchorId);
            ui = [self parseXMLDocument:node havingUI:ui anchor:anchorId updateType:updateType];
            if (ui) [self renderDOM:ui anchor:remoteMenu];
        }

    }
}

// use with extreme caution, because this function get called everytime
- (void)windowDidUpdate:(NSNotification *) notification {
    //close opened menu
    if (renderer && renderer.selectedMenuModel) {
        do{
            [sharedConnection sendActionAt:renderer.selectedMenuModel.unique_id actionName:@"collapse"];
            renderer.selectedMenuModel = renderer.selectedMenuModel.parent;
        } while (renderer.selectedMenuModel && ([renderer.selectedMenuModel.type isEqualToString:@"menuitem"] ||
                                                [renderer.selectedMenuModel.type isEqualToString:@"menu"])) ;
        renderer.selectedMenuModel = nil;
    }
    
    if( currentResponder != [[notification object] firstResponder]){
        currentResponder = [[notification object] firstResponder];
        
        if ([currentResponder isKindOfClass:[NSSearchField class]]){
            [renderer sendFocus:(NSView*)currentResponder];
        }
        if([currentResponder isKindOfClass:[NSPathControl class]]){
            //[renderer sendFocusToSynchronize:(NSView*) currentResponder];
            [renderer sendFocus:(NSView*) currentResponder];
        }
        if([currentResponder isKindOfClass:[NSComboBox class]]){
            //[renderer sendFocusToSynchronize:(NSView*) currentResponder];
            [renderer sendFocus:(NSView*) currentResponder];
        }
    }
}

@end

