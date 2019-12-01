#include "MapleCodeReader.h"

using namespace MapleCode::Reader;

static const std::uint32_t SizeModeToSize[] = { 0, 1, 2, 4 };

static std::uint32_t ReadNumberU(const void* data, std::uint32_t* pPos, int size)
{
	const char* data8 = static_cast<const char*>(data);
	std::uint32_t ret = 0;
	std::memcpy(&ret, data8 + *pPos, size);
	*pPos += size;
	return ret;
}

template <typename T, typename RET = std::int32_t>
static RET ConvertNumber(std::uint32_t val)
{
	T ret = {};
	std::memcpy(&ret, &val, sizeof(T));
	return static_cast<RET>(ret);
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

static NodeType* GetNodeType(DocumentData* doc, std::uint32_t offset)
{
	std::uint32_t pos = doc->NodeRange.Start + offset;
	auto typeIndex = ReadNumberU(doc->Data.get(), &pos, doc->TypeWidth);
	if (typeIndex > doc->TypeList.size())
	{
		throw ReaderException("Invalid node type");
	}
	return &doc->TypeList[typeIndex];
}

static std::uint32_t GetNextNode(DocumentData* doc, std::uint32_t offset)
{
	auto type = GetNodeType(doc, offset);
	if (type->HasChildren())
	{
		std::uint32_t pos = doc->NodeRange.Start + offset + type->GetTotalLen();
		auto childrenLen = ReadNumberU(doc->Data.get(), &pos, doc->NodeWidth);
		return offset + type->GetTotalLen() + doc->NodeWidth + childrenLen;
	}
	return offset + type->GetTotalLen();
}

static bool ValidateNodeOffset(DocumentData* doc, std::uint32_t offset)
{
	auto type = GetNodeType(doc, offset);
	auto nodeEnd = offset + type->GetTotalLen();
	if (type->HasChildren())
	{
		nodeEnd += doc->NodeWidth;
	}
	return nodeEnd <= doc->NodeRange.GetLength();
}

std::unique_ptr<Document> Document::ReadFromData(Document* typeListDoc, const void* data, std::uint32_t length)
{
	const std::uint8_t* data8 = static_cast<const uint8_t*>(data);
	std::uint8_t sizeMode = data8[0];

	std::uint32_t strWidth = SizeModeToSize[(sizeMode >> 0) & 3];
	std::uint32_t typeWidth = SizeModeToSize[(sizeMode >> 2) & 3];
	std::uint32_t nodeWidth = SizeModeToSize[(sizeMode >> 4) & 3];
	std::uint32_t dataWidth = SizeModeToSize[(sizeMode >> 6) & 3];

	DocumentData::TableRange strRange, typeRange, nodeRange, dataRange;
	std::uint32_t readSizePos = 1;
	strRange.End = ReadNumberU(data, &readSizePos, strWidth);
	typeRange.End = ReadNumberU(data, &readSizePos, typeWidth);
	nodeRange.End = ReadNumberU(data, &readSizePos, nodeWidth);
	dataRange.End = ReadNumberU(data, &readSizePos, dataWidth);

	if (typeListDoc == nullptr && typeRange.End == 0 && nodeRange.End != 0)
	{
		throw ReaderException("No node type list specified");
	}
	if (typeListDoc != nullptr && typeRange.End != 0)
	{
		throw ReaderException("Cannot specify external type list for document with internal type list");
	}

	typeRange.Move(strRange.End);
	nodeRange.Move(typeRange.End);
	dataRange.Move(nodeRange.End);

	auto headerLength = 1 + strWidth + typeWidth + nodeWidth + dataWidth;
	auto totalLength = headerLength + dataRange.End;
	if (length < totalLength)
	{
		throw ReaderException("Cannot read to the end of document");
	}

	auto content = std::make_unique<std::uint8_t[]>(dataRange.End);
	std::memcpy(content.get(), data8 + headerLength, dataRange.End);
	
	std::vector<std::string> stringTable;
	for (auto pos = strRange.Start; pos < strRange.End; )
	{
		std::int32_t dataOffset = dataRange.Start + ReadNumberU(content.get(), &pos, dataWidth);
		std::string str;
		if (!ReadString(content.get() + dataOffset, content.get() + dataRange.End, str))
		{
			throw ReaderException("Invalid string data");
		}
		stringTable.push_back(str);
	}

	std::vector<NodeType> typeList;
	std::vector<std::uint32_t> argSizes = {
		1, 2, 4, 1, 2, 4, 4,
		strWidth, dataWidth * 2, nodeWidth, nodeWidth + strWidth,
	};
	if (typeListDoc != nullptr)
	{
		typeList = typeListDoc->Data.TypeList;
	}
	else
	{
		for (auto pos = typeRange.Start; pos < typeRange.End; )
		{
			std::uint32_t strIndex = ReadNumberU(content.get(), &pos, strWidth);
			if (strIndex < 0 || strIndex > stringTable.size())
			{
				throw ReaderException("Invalid string index");
			}
			std::uint32_t dataOffset = ReadNumberU(content.get(), &pos, dataWidth);
			if (dataOffset < 0 || dataOffset >= dataRange.GetLength())
			{
				throw ReaderException("Invalid data offset");
			}
			std::uint8_t genericCount = content[pos++];
			std::uint8_t hasChild = content[pos++];
			std::uint8_t argCount = content[dataRange.Start + dataOffset];
			if (dataOffset + 1 + argCount > dataRange.GetLength())
			{
				throw ReaderException("Invalid data offset");
			}
			std::vector<NodeArgumentType> args;
			args.resize(argCount);
			std::memcpy(args.data(), content.get() + dataRange.Start + dataOffset + 1, argCount);
			std::uint32_t nodeLen = typeWidth;
			nodeLen += strWidth * genericCount;
			for (auto tt : args)
			{
				nodeLen += argSizes[(int)tt];
			}
			typeList.emplace_back(stringTable[strIndex], genericCount, std::move(args), hasChild != 0, nodeLen);
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
	ret->Data.ArgumentWidth = std::move(argSizes);

	return ret;
}

NodeRange::NodeIterator& NodeRange::NodeIterator::operator++()
{
	_offset = GetNextNode(_document, _offset);
	_internalNode = { _document, _offset };
	return *this;
}

void NodeRange::NodeIterator::ValidateInternalNode() const
{
	if (!ValidateNodeOffset(_document, _offset))
	{
		throw ReaderException("Invalid node data");
	}
}

NodeType* Node::GetNodeType() const
{
	return ::GetNodeType(_document, _offset);
}

void Node::ReadGenericArguments(std::vector<std::string>& results) const
{
	auto type = GetNodeType();
	results.clear();

	std::uint32_t pos = _document->NodeRange.Start + _offset + _document->TypeWidth;
	for (std::uint32_t i = 0; i < type->GetGenericArgCount(); ++i)
	{
		auto strIndex = ReadNumberU(_document->Data.get(), &pos, _document->StrWidth);
		if (strIndex > _document->StrList.size())
		{
			throw ReaderException("Invalid string index");
		}
		results.push_back(_document->StrList[strIndex]);
	}
}

void Node::ReadArguments(std::vector<NodeArgument>& results) const
{
	auto type = GetNodeType();
	results.clear();

	std::uint32_t o = _offset + _document->TypeWidth +
		_document->StrWidth * type->GetGenericArgCount();
	auto& argTypes = type->GetArgumentTypes();
	for (std::uint32_t i = 0; i < argTypes.size(); ++i)
	{
		auto tt = argTypes[i];
		results.push_back({ _document, tt, o });
		o += _document->ArgumentWidth[(int)tt];
	}
	if (o > _document->NodeRange.GetLength())
	{
		throw ReaderException("Invalid node data");
	}
}

NodeRange Node::GetChildren() const
{
	auto type = GetNodeType();
	std::uint32_t cstart = _offset + type->GetTotalLen();
	std::uint32_t pos = _document->NodeRange.Start + cstart;
	if (type->HasChildren())
	{
		auto clen = ReadNumberU(_document->Data.get(), &pos, _document->NodeWidth);
		if (clen + _offset > _document->NodeRange.GetLength())
		{
			throw ReaderException("Invalid node data");
		}
		cstart += _document->NodeWidth;
		return { _document, cstart, cstart + clen };
	}
	return { _document, cstart, cstart };
}

static Node FindParentInternal(DocumentData* doc, std::uint32_t offset, std::uint32_t start)
{
	auto type = GetNodeType(doc, start);
	if (!type->HasChildren())
	{
		throw ReaderException("Invalid node hierarchy");
	}

	std::uint32_t child = start + type->GetTotalLen() + doc->NodeWidth;
	if (offset < child)
	{
		throw ReaderException("Invalid node hierarchy");
	}
	std::uint32_t childEnd = GetNextNode(doc, child);

	while (1)
	{
		if (child == offset) return { doc, start };
		if (child < offset && offset < childEnd)
		{
			return FindParentInternal(doc, offset, child);
		}
		child = childEnd;
		childEnd = GetNextNode(doc, child);
	}
}

Node Node::FindParent() const
{
	if (_offset > _document->NodeRange.GetLength())
	{
		throw ReaderException("Invalid node offset");
	}
	std::uint32_t begin = 0, end = GetNextNode(_document, 0);
	while (1)
	{
		if (_offset == begin) return { nullptr, 0 };
		if (begin < _offset && _offset < end)
		{
			return FindParentInternal(_document, _offset, begin);
		}
		begin = end;
		end = GetNextNode(_document, begin);
	}
}

std::int32_t NodeArgument::GetSigned()
{
	switch (_type)
	{
	case NodeArgumentType::S8:
		return ConvertNumber<std::int8_t>(ReadArgNumber(1));
	case NodeArgumentType::S16:
		return ConvertNumber<std::int16_t>(ReadArgNumber(2));
	case NodeArgumentType::S32:
		return ConvertNumber<std::int32_t>(ReadArgNumber(4));
	}
	throw ReaderException("Incorrect argument type");
}

std::uint32_t NodeArgument::GetUnsigned()
{
	switch (_type)
	{
	case NodeArgumentType::U8:
		return ReadArgNumber(1);
	case NodeArgumentType::U16:
		return ReadArgNumber(2);
	case NodeArgumentType::U32:
		return ReadArgNumber(4);
	}
	throw ReaderException("Incorrect argument type");
}

std::string NodeArgument::GetString()
{
	if (_type != NodeArgumentType::STR)
	{
		throw ReaderException("Incorrect argument type");
	}
	auto index = ReadArgNumber(_document->StrWidth);
	if (index > _document->StrList.size())
	{
		throw ReaderException("Invalid string index");
	}
	return _document->StrList[index];
}

float NodeArgument::GetFloat()
{
	if (_type != NodeArgumentType::F32)
	{
		throw ReaderException("Incorrect argument type");
	}
	return ConvertNumber<float, float>(ReadArgNumber(4));
}

Node NodeArgument::GetNode()
{
	if (_type != NodeArgumentType::REF)
	{
		throw ReaderException("Incorrect argument type");
	}
	auto node = ReadArgNumber(_document->NodeWidth);
	if (!ValidateNodeOffset(_document, node))
	{
		throw ReaderException("Invalid node data");
	}
	return { _document, node };
}

std::tuple<Node, std::string> MapleCode::Reader::NodeArgument::GetField()
{
	std::uint32_t pos = _document->NodeRange.Start + _offset;
	auto node = ReadNumberU(_document->Data.get(), &pos, _document->NodeWidth);
	if (!ValidateNodeOffset(_document, node))
	{
		throw ReaderException("Invalid node data");
	}

	auto field = ReadNumberU(_document->Data.get(), &pos, _document->StrWidth);
	if (field > _document->StrList.size())
	{
		throw ReaderException("Invalid string index");
	}
	return { { _document, node }, _document->StrList[field] };
}

std::uint32_t NodeArgument::ReadArgNumber(int size)
{
	std::uint32_t pos = _document->NodeRange.Start + _offset;
	return ReadNumberU(_document->Data.get(), &pos, size);
}

void NodeArgument::GetDataRange(std::uint32_t* pBegin, std::uint32_t* pEnd)
{
	std::uint32_t pos = _document->NodeRange.Start + _offset;
	*pBegin = ReadNumberU(_document->Data.get(), &pos, _document->DataWidth);
	*pEnd = ReadNumberU(_document->Data.get(), &pos, _document->DataWidth);
}

void NodeArgument::FillData(void* buffer, std::uint32_t begin, std::uint32_t end)
{
	if (end < begin || end > _document->DataRange.End)
	{
		throw ReaderException("Invalid data offset");
	}
	std::memcpy(buffer, _document->Data.get() + _document->DataRange.Start + begin, end - begin);
}
