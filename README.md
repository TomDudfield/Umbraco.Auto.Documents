Umbraco Auto Folders
====================

Creates folders automatically based on a date property on a document type
eg /2012/11/05/Item

The alias of the document type to create the auto folders for
`<add key="autofolders:ItemDocType" value="false" />`

The alias of the date folder document type
`<add key="autofolders:DateFolderDocType" value="dateFolder" />`

The alias of the date property to base the folders on
`<add key="autofolders:ItemDateProperty" value="itemDate" />`

To toggle whether folders are created for the day
`<add key="autofolders:CreateDayFolders" value="false" />`