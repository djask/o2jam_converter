#include <iostream>
#include <fstream>
#include <string>
#include <memory>

#include "ojmDumper.h"
#include "pch.h"

namespace ojmDumper {
	void dumpOJM(std::string path) {
		std::ifstream file(path,
			std::ios::binary | std::ios::ate);

		std::streamsize size = file.tellg();
		file.seekg(0, std::ios::beg);

		ojmDumper::ojnHeader header;
		if (file.read((char *)&header, sizeof(ojmDumper::ojnHeader)))
		{
			std::cout << "Sig " << header.sig << std::endl;
			std::cout << "WavC " << header.wavCount << std::endl;
			std::cout << "OggC " << header.oggCount << std::endl;
			std::cout << "WavOfft " << header.wavOffset << std::endl;
			std::cout << "OggOfft " << header.oggOffset << std::endl;
		}

		//read wav
		while (file.tellg() < header.oggOffset) {
			//not implemented
			std::cout << "----------- GOT WAV -----------" << std::endl;
			file.seekg(header.oggOffset, std::ios_base::beg);
		}

		//read ogg
		while (file.tellg() < size) {
			std::shared_ptr<ojmDumper::oggHeader> oggInfo(new ojmDumper::oggHeader);
			if (file.read((char*)oggInfo.get(), sizeof(ojmDumper::oggHeader))) {
				std::cout << "----------- GOT OGG -----------" << std::endl;
				std::cout << "Song Name " << oggInfo->name << std::endl;
				std::cout << "size " << oggInfo->fileSize << std::endl;
			}
			//pretend to read in file lol
			file.seekg(oggInfo->fileSize, std::ios_base::cur);
		}
	}
}