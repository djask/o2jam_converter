#include "pch.h"
#include <filesystem>
#include <iostream>

#include "ojmDumper.h"

namespace fs = std::experimental::filesystem;

int main()
{
	std::string path("D:\\Games\\o2servers\\O2jamv3\\Music");
	std::string ext(".ojm");
	for (auto& p : fs::recursive_directory_iterator(path))
	{
		if (p.path().extension() == ext)
			ojmDumper::dumpOJM(p.path().string());
		std::system("pause");
	}
	return 0;
}