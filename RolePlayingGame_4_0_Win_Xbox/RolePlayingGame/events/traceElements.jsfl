var myElements = fl.getDocumentDOM().getTimeline().layers[0].frames[0].elements; 


var lays = fl.getDocumentDOM().getTimeline().layers

var ind = 0

var name = ""

for(i in lays)
{
	//fl.outputPanel.trace(lays[i].name)
	var frams = lays[i].frames
	
	for(j in frams)
	{
		ind++;
		
		var elems = frams[j].elements
		
		for(k in elems)
		{	
			if(elems[k].libraryItem.name != null)
			{
				if(name != elems[k].name + "; " + elems[k].libraryItem.name.substr(elems[k].libraryItem.name.lastIndexOf('/')+1,elems[k].libraryItem.name.length))
				{
					name = elems[k].name + "; " + elems[k].libraryItem.name.substr(elems[k].libraryItem.name.lastIndexOf('/')+1,elems[k].libraryItem.name.length)
					fl.outputPanel.trace(ind + "; " + elems[k].name + "; " + elems[k].libraryItem.name.substr(elems[k].libraryItem.name.lastIndexOf('/')+1,elems[k].libraryItem.name.length))
				}
			}
		}
	}
	
	ind = 0;
}

//fl.outputPanel.save("file:///C:/flashlogs.txt");
