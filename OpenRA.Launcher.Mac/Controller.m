/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */

#import "Controller.h"
#import "Mod.h"
#import "SidebarEntry.h"
#import "GameInstall.h"
#import "ImageAndTextCell.h"
#import "JSBridge.h"
#import "Download.h"

@implementation Controller
@synthesize allMods;
@synthesize webView;

+ (void)initialize
{
	[[NSUserDefaults standardUserDefaults]
	 registerDefaults:[NSDictionary dictionaryWithObject:[[NSBundle mainBundle] resourcePath]
												  forKey:@"gamepath"]];
}

- (void)awakeFromNib
{	
	NSString *gamePath = [[NSUserDefaults standardUserDefaults] stringForKey:@"gamepath"];

	game = [[GameInstall alloc] initWithPath:gamePath];
	[[JSBridge sharedInstance] setController:self];
	downloads = [[NSMutableDictionary alloc] init];
	hasMono = [self hasSupportedMono];
	if (hasMono)
	{
		NSTableColumn *col = [outlineView tableColumnWithIdentifier:@"mods"];
		ImageAndTextCell *imageAndTextCell = [[[ImageAndTextCell alloc] init] autorelease];
		[col setDataCell:imageAndTextCell];
		
		sidebarItems = [[SidebarEntry headerWithTitle:@""] retain];
		[self populateModInfo];
		id modsRoot = [self sidebarModsTree];
		[sidebarItems addChild:modsRoot];
		id otherRoot = [self sidebarOtherTree];
		[sidebarItems addChild:otherRoot];
		
		
		[outlineView reloadData];
		[outlineView expandItem:modsRoot expandChildren:YES];
		
		if ([[modsRoot children] count] > 0)
		{
			id firstMod = [[modsRoot children] objectAtIndex:0];
			int row = [outlineView rowForItem:firstMod];
			[outlineView selectRowIndexes:[NSIndexSet indexSetWithIndex:row] byExtendingSelection:NO];
			[[webView mainFrame] loadRequest:[NSURLRequest requestWithURL: [firstMod url]]];
		}
		
		[outlineView expandItem:otherRoot expandChildren:YES];
	}
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	if (!hasMono)
	{
		NSAlert *alert = [NSAlert alertWithMessageText:@"Mono Framework"
										 defaultButton:@"Download Mono"
									   alternateButton:@"Quit"
										   otherButton:nil
							 informativeTextWithFormat:@"OpenRA requires the Mono Framework version 2.6.7 or later."];
		
		[alert beginSheetModalForWindow:window modalDelegate:self didEndSelector:@selector(monoAlertEnded:code:context:) contextInfo:NULL];
	}
}

- (void)monoAlertEnded:(NSAlert *)alert
				  code:(int)button
			   context:(void *)v
{
	if (button == NSAlertDefaultReturn)
		[[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"http://www.go-mono.com/mono-downloads/download.html"]];
	
	[[NSApplication sharedApplication] terminate:self];
}

- (BOOL)hasSupportedMono
{
	if (![[NSFileManager defaultManager] fileExistsAtPath:@"/Library/Frameworks/Mono.framework/Commands/mono"])
		return NO;
	
	NSPipe *outPipe = [NSPipe pipe];
	NSTask *task = [[NSTask alloc] init];
    [task setLaunchPath:@"/Library/Frameworks/Mono.framework/Commands/mono"];
    [task setArguments:[NSMutableArray arrayWithObject:@"--version"]];
	[task setStandardOutput:outPipe];
	[task setStandardError:[task standardOutput]];
    [task launch];
	
	NSData *data = [[outPipe fileHandleForReading] readDataToEndOfFile];
	[task waitUntilExit];
    [task release];
	
	NSString *ret = [[NSString alloc] initWithData:data encoding:NSASCIIStringEncoding];
	
	int major = 0;
	int minor = 0;
	int point = 0;
	sscanf([ret UTF8String], "Mono JIT compiler version %d.%d.%d", &major, &minor, &point);
	[ret release];
	
	return (major > 2 ||
			(major == 2 && minor > 6) ||
			(major == 2 && minor == 6 && point >= 7));
}

- (void)dealloc
{
	[sidebarItems release]; sidebarItems = nil;
	[downloads release]; downloads = nil;
	[super dealloc];
}

- (void)populateModInfo
{
	// Get info for all installed mods
	[allMods autorelease];
	allMods = [[game infoForMods:[game installedMods]] retain];	
}

- (SidebarEntry *)sidebarModsTree
{
	SidebarEntry *rootItem = [SidebarEntry headerWithTitle:@"MODS"];
	for (id key in allMods)
	{	
		id aMod = [allMods objectForKey:key];
		if ([aMod standalone])
		{
			id path = [[game gamePath] stringByAppendingPathComponent:@"mods"];
			id child = [SidebarEntry entryWithMod:aMod allMods:allMods baseURL:[NSURL URLWithString:path]];
			[rootItem addChild:child];
		}
	}
	
	return rootItem;
}

- (SidebarEntry *)sidebarOtherTree
{
	SidebarEntry *rootItem = [SidebarEntry headerWithTitle:@"OTHER"];
	[rootItem addChild:[SidebarEntry entryWithTitle:@"Support" url:nil icon:nil]];
	[rootItem addChild:[SidebarEntry entryWithTitle:@"Credits" url:nil icon:nil]];
	
	return rootItem;
}

- (void)launchMod:(NSString *)mod
{
	[game launchMod:mod];
}

- (BOOL)registerDownload:(NSString *)key withURL:(NSString *)url filePath:(NSString *)path;
{
	if ([downloads objectForKey:key] != nil)
		return NO;
	
	[downloads setObject:[Download downloadWithURL:url filename:path key:key game:game]
				  forKey:key];
	return YES;
}

- (Download *)downloadWithKey:(NSString *)key
{
	return [downloads objectForKey:key];
}

#pragma mark Sidebar Datasource and Delegate
- (NSInteger)outlineView:(NSOutlineView *)anOutlineView numberOfChildrenOfItem:(id)item
{
	// Can be called before awakeFromNib; return nothing
	if (sidebarItems == nil)
		return 0;
	
	// Root item
	if (item == nil)
		return [[sidebarItems children] count];

	return [[item children] count];
}

- (BOOL)outlineView:(NSOutlineView *)outlineView isItemExpandable:(id)item
{
	return (item == nil) ? YES : [[item children] count] != 0;
}

- (id)outlineView:(NSOutlineView *)outlineView
			child:(NSInteger)index
		   ofItem:(id)item
{
	if (item == nil)
		return [[sidebarItems children] objectAtIndex:index];
	
	return [[item children] objectAtIndex:index];
}

-(BOOL)outlineView:(NSOutlineView*)outlineView isGroupItem:(id)item
{	
	if (item == nil)
		return NO;
	
	return [item isHeader];
}

- (id)outlineView:(NSOutlineView *)outlineView
objectValueForTableColumn:(NSTableColumn *)tableColumn
		   byItem:(id)item
{
	return [item title];
}

- (BOOL)outlineView:(NSOutlineView *)outlineView shouldSelectItem:(id)item;
{	
	// don't allow headers to be selected
	if ([item isHeader] || [item url] == nil)
		return NO;
	
	[[webView mainFrame] loadRequest:[NSURLRequest requestWithURL:[item url]]];

	return YES;
}

- (void)outlineView:(NSOutlineView *)olv willDisplayCell:(NSCell*)cell forTableColumn:(NSTableColumn *)tableColumn item:(id)item
{
	if ([[tableColumn identifier] isEqualToString:@"mods"])
	{
		if ([cell isKindOfClass:[ImageAndTextCell class]])
		{
			[(ImageAndTextCell*)cell setImage:[item icon]];
		}
	}
}

#pragma mark WebView delegates
- (void)webView:(WebView *)sender didClearWindowObject:(WebScriptObject *)windowObject forFrame:(WebFrame *)frame
{
	[windowObject setValue:[JSBridge sharedInstance] forKey:@"external"];
}

- (void)webView:(WebView *)webView addMessageToConsole:(NSDictionary *)dictionary
{
	NSLog(@"%@",dictionary);
}


#pragma mark Application delegates
- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender
{
	return YES;
}

- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender
{
	int count = 0;
	for (NSString *key in downloads)
		if ([[(Download *)[downloads objectForKey:key] status] isEqualToString:@"DOWNLOADING"])
			count++;
	
	if (count == 0)
		return NSTerminateNow;
	
	NSString *format = count == 1 ? @"1 download is" : [NSString stringWithFormat:@"%d downloads are",count];
	NSAlert *alert = [NSAlert alertWithMessageText:@"Are you sure you want to quit?"
									 defaultButton:@"Wait"
								   alternateButton:@"Quit"
									   otherButton:nil
						 informativeTextWithFormat:@"%@ in progress and will be cancelled if you quit.", format];
	
	[alert beginSheetModalForWindow:window modalDelegate:self didEndSelector:@selector(quitAlertEnded:code:context:) contextInfo:NULL];
	return NSTerminateLater;
}

- (void)quitAlertEnded:(NSAlert *)alert
			  code:(int)button
		   context:(void *)v
{
	NSApplicationTerminateReply reply = (button == NSAlertDefaultReturn) ? NSTerminateCancel : NSTerminateNow;
	[[NSApplication sharedApplication] replyToApplicationShouldTerminate:reply];
}

- (void)applicationWillTerminate:(NSNotification *)aNotification
{
	// Cancel all in-progress downloads
	for (NSString *key in downloads)
	{	
		Download *d = [downloads objectForKey:key];
		if ([[d status] isEqualToString:@"DOWNLOADING"])
			[d cancel];
	}
}

@end
