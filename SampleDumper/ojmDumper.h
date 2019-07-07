#include <string>

#pragma once

namespace ojmDumper {
	typedef struct _ojnHeader {
		char sig[4];
		short wavCount;
		short oggCount;
		int wavOffset;
		int oggOffset;
		int chunk;
	} ojnHeader;

	typedef struct _oggHeader {
		char name[32];
		int fileSize;
	} oggHeader;

	void dumpOJM(std::string path);
}
