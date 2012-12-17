Umbraco Auto Documents
======================

Creates documents automatically based on a date property on the document type
eg /2012/11/05/Item

The alias of the document type to create the auto documents for
`<add key="AutoDocuments:ItemDocumentTypes" value="BlogPost" />`

The alias of the date document type
`<add key="AutoDocuments:DateDocumentType" value="DateFolder" />`

The alias of the date property to base the date documents on
`<add key="AutoDocuments:ItemDatePropertyAlias" value="itemDate" />`

To toggle whether documents are created for the day
`<add key="AutoDocuments:CreateDayDocuments" value="false" />`