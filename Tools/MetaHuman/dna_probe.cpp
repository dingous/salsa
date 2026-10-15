#include <type_traits>
#include <riglogic/RigLogic.h>
#include <cmath>
#include <iostream>
#include <fstream>

// Preflight only: evaluates official DNA without claiming a rendered character.
int main(int argc, char** argv) {
    if (argc != 2) { std::cerr << "Usage: MetaHumanDnaProbe DNA_FILE\n"; return 2; }
    std::ifstream file(argv[1], std::ios::binary | std::ios::ate);
    if (!file || file.tellg() <= 0 || file.tellg() > 150 * 1024 * 1024) {
        std::cerr << "DNA file missing, empty or over 150 MB\n"; return 2;
    }
    auto stream = rl4::makeScoped<rl4::FileStream>(argv[1], rl4::FileStream::AccessMode::Read, rl4::FileStream::OpenMode::Binary);
    auto reader = rl4::makeScoped<rl4::BinaryStreamReader>(stream.get());
    reader->read();
    if (!rl4::Status::isOk()) { std::cerr << "Invalid or unsupported DNA: " << rl4::Status::get().message << '\n'; return 3; }
    auto logic = rl4::makeScoped<rl4::RigLogic>(reader.get());
    auto instance = rl4::makeScoped<rl4::RigInstance>(logic.get());
    float peak = 0; unsigned long outputValues = 0;
    for (int step = 0; step < 5; ++step) {
        for (std::uint16_t i = 0; i < instance->getRawControlCount(); ++i) instance->setRawControl(i, step / 4.f);
        logic->calculate(instance.get());
        for (auto values : {instance->getJointOutputs(), instance->getBlendShapeOutputs(), instance->getAnimatedMapOutputs()}) {
            for (float value : values) {
                if (!std::isfinite(value)) { std::cerr << "Non-finite rig output\n"; return 4; }
                peak = std::fmax(peak, std::fabs(value)); ++outputValues;
            }
        }
    }
    unsigned long vertices = 0;
    for (std::uint16_t i = 0; i < reader->getMeshCount(); ++i) vertices += reader->getVertexPositionCount(i);
    std::cout << "{\"engine\":\"Epic OpenRigLogic\",\"rawControls\":" << reader->getRawControlCount()
              << ",\"joints\":" << reader->getJointCount() << ",\"meshes\":" << reader->getMeshCount()
              << ",\"vertices\":" << vertices << ",\"blendShapeChannels\":" << reader->getBlendShapeChannelCount()
              << ",\"evaluationSteps\":5,\"finiteOutputValues\":" << outputValues << ",\"peakOutput\":" << peak
              << ",\"hasGeometry\":" << (vertices > 0 ? "true" : "false") << ",\"renderedAvatar\":false}\n";
    return 0;
}
