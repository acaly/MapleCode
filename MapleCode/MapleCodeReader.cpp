#include "MapleCodeReader.h"

using namespace MapleCode::Reader;

static const std::int32_t SizeModeToSize[] = { 0, 1, 2, 4 };

static std::uint32_t ReadNumberU(const void* data, std::int32_t* pPos, int size)
{
	const char* data8 = static_cast<const char*>(data);
	std::uint32_t ret = 0;
	std::memcpy(&ret, data8 + *pPos, size);
	*pPos += size;
	return ret;
}

static std::uint32_t ReadNumber(const void* data, std::int32_t* pPos, int size)
{
	const char* data8 = static_cast<const char*>(data);
	std::int32_t ret = 0;
	std::memcpy(&ret, data8 + *pPos, size);
	*pPos += size;
	return ret;
}

static bool ReadString(const uint8_t* data, const uint8_t* dataEnd, std::string& result)
{
	auto strEnd = std::memchr(data, 0, dataEnd - data);
	auto strEndChar = static_cast<const uint8_t*>(strEnd);
	if (strEndChar == nullptr)
	{
		return false;
	}
	result = std::string(data, strEndChar);
	return true;
}

std::unique_ptr<Document> Document::ReadFromData(Document* typeListDoc, const void* data, int length)
{
	const std::uint8_t* data8 = static_cast<const uint8_t*>(data);
	std::uint8_t sizeMode = data8[0];

	std::int32_t strWidth = SizeModeToSize[(sizeMode >> 0) & 3];
	std::int32_t typeWidth = SizeModeToSize[(sizeMode >> 2) & 3];
	std::int32_t nodeWidth = SizeModeToSize[(sizeMode >> 4) & 3];
	std::int32_t dataWidth = SizeModeToSize[(sizeMode >> 6) & 3];

	DocumentData::TableRange strRange = {}, typeRange = {}, nodeRange = {}, dataRange = {};
	int readSizePos = 1;
	strRange.End = ReadNumber(data, &readSizePos, strWidth);
	typeRange.End = ReadNumber(data, &readSizePos, typeWidth);
	nodeRange.End = ReadNumber(data, &readSizePos, nodeWidth);
	dataRange.End = ReadNumber(data, &readSizePos, dataWidth);

	if (typeListDoc == nullptr && typeRange.End == 0 && nodeRange.End != 0)
	{
		//No type list.
		return nullptr;
	}
	if (typeListDoc != nullptr && typeRange.End != 0)
	{
		//Two type lists.
		return nullptr;
	}

	typeRange.Move(strRange.End);
	nodeRange.Move(typeRange.End);
	dataRange.Move(nodeRange.End);

	auto headerLength = 1 + strWidth + typeWidth + nodeWidth + dataWidth;
	auto totalLength = headerLength + dataRange.End;
	if (length < totalLength)
	{
		return nullptr;
	}

	auto content = std::make_unique<std::uint8_t[]>(dataRange.End);
	std::memcpy(content.get(), data8 + headerLength, dataRange.End);
	
	std::vector<std::string> stringTable;
	for (int pos = strRange.Start; pos < strRange.End; )
	{
		std::int32_t dataOffset = dataRange.Start + ReadNumber(content.get(), &pos, dataWidth);
		std::string str;
		if (!ReadString(content.get() + dataOffset, content.get() + dataRange.End, str))
		{
			return nullptr;
		}
		stringTable.push_back(str);
	}

	std::vector<NodeType> typeList;
	if (typeListDoc != nullptr)
	{
		typeList = typeListDoc->Data.TypeList;
	}
	else
	{
		for (int pos = typeRange.Start; pos < typeRange.End; )
		{
			//TODO validate strIndex, dataOffset, argCount
			std::int32_t strIndex = ReadNumber(content.get(), &pos, strWidth);
			std::int32_t dataOffset = ReadNumber(content.get(), &pos, dataWidth);
			std::uint8_t genericCount = content[pos++];
			std::uint8_t hasChild = content[pos++];
			std::uint8_t argCount = content[dataRange.Start + dataOffset];
			//auto args = std::make_unique<NodeArgumentType[]>(argCount);
			std::vector<NodeArgumentType> args;
			args.resize(argCount);
			std::memcpy(args.data(), content.get() + dataRange.Start + dataOffset + 1, argCount);
			typeList.emplace_back(stringTable[strIndex], genericCount, std::move(args), hasChild != 0);
		}
	}

	auto ret = std::make_unique<Document>();
	ret->Data.Data = std::move(content);
	ret->Data.StrWidth = strWidth;
	ret->Data.TypeWidth = typeWidth;
	ret->Data.NodeWidth = nodeWidth;
	ret->Data.DataWidth = dataWidth;
	ret->Data.StrList = std::move(stringTable);
	ret->Data.TypeList = std::move(typeList);
	ret->Data.StrRange = strRange;
	ret->Data.TypeRange = typeRange;
	ret->Data.NodeRange = nodeRange;
	ret->Data.DataRange = dataRange;
	return ret;
}
