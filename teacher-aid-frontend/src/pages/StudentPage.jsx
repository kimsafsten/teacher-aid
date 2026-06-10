import QuestionForm from '../components/QuestionForm'

export default function StudentPage() {
  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <h1 className="text-3xl font-bold text-center mb-6">Kursassistent</h1>
      <QuestionForm />
    </div>
  )
}
